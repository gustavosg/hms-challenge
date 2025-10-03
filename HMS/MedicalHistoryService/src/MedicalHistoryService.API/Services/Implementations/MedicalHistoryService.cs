using System.Linq.Expressions;

using MedicalHistoryService.API.Data;
using MedicalHistoryService.API.Models;
using MedicalHistoryService.API.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

using Shared.DTOs;
using Shared.Infra.UnitOfWork;
using Shared.Mapper;
using Shared.Services.Cache;

namespace MedicalHistoryService.API.Services.Implementations;

public class MedicalHistoryService(
    IAddMapper<Shared.DTOs.MedicalHistory.Add.Request, Models.MedicalHistory> addMapper,
    IEditMapper<Shared.DTOs.MedicalHistory.Edit.Request, Models.MedicalHistory> editMapper,
    IGetMapper<Models.MedicalHistory, Shared.DTOs.MedicalHistory.Get.Response> getMapper,
    IAddMapper<Shared.DTOs.MedicalHistory.Add.DiagnosisRequest, Models.Diagnosis> diagnosisAddMapper,
    IAddMapper<Shared.DTOs.MedicalHistory.Add.ExamRequest, Models.Exam> examAddMapper,
    IAddMapper<Shared.DTOs.MedicalHistory.Add.PrescriptionRequest, Models.Prescription> prescriptionAddMapper,
    IEditMapper<Shared.DTOs.MedicalHistory.Edit.DiagnosisRequest, Models.Diagnosis> diagnosisEditMapper,
    IEditMapper<Shared.DTOs.MedicalHistory.Edit.ExamRequest, Models.Exam> examEditMapper,
    IEditMapper<Shared.DTOs.MedicalHistory.Edit.PrescriptionRequest, Models.Prescription> prescriptionEditMapper,
    IUnitOfWork<ApplicationDbContext> unitOfWork,
    ICacheService cache)
    : IMedicalHistoryService
{
    private const string MEDICAL_HISTORY_CACHE_KEY = "medical-history:{0}";
    private const string MEDICAL_HISTORIES_PAGE_CACHE_KEY = "medical-histories:page:{0}:{1}";
    private const string MEDICAL_HISTORIES_FILTERED_CACHE_KEY = "medical-histories:filtered:{0}:page:{1}:{2}";
    private const string PATIENT_MEDICAL_HISTORY_CACHE_KEY = "patient-medical-history:{0}";
    private const string PATIENT_DOC_MEDICAL_HISTORY_CACHE_KEY = "patient-doc-medical-history:{0}";

    public async Task<Guid> AddAsync(Shared.DTOs.MedicalHistory.Add.Request request)
    {
        MedicalHistory medicalHistory = addMapper.ToEntity(request);

        if (request.Diagnosis != null)
        {
            var diagnosis = diagnosisAddMapper.ToEntity(request.Diagnosis);
            diagnosis.MedicalHistoryId = medicalHistory.Id;
            medicalHistory.Diagnoses.Add(diagnosis);
        }

        if (request.Exam != null)
        {
            var exam = examAddMapper.ToEntity(request.Exam);
            exam.MedicalHistoryId = medicalHistory.Id;
            medicalHistory.Exams.Add(exam);
        }

        if (request.Prescription != null)
        {
            var prescription = prescriptionAddMapper.ToEntity(request.Prescription);
            prescription.MedicalHistoryId = medicalHistory.Id;
            medicalHistory.Prescriptions.Add(prescription);
        }

        await unitOfWork.Context.MedicalHistories.AddAsync(medicalHistory);
        await unitOfWork.CommitAsync();

        InvalidateListCaches();
        InvalidatePatientCaches(medicalHistory.PatientId, medicalHistory.Document);

        return medicalHistory.Id;
    }

    public async Task<Shared.DTOs.MedicalHistory.Get.Response> EditAsync(Shared.DTOs.MedicalHistory.Edit.Request request)
    {
        MedicalHistory medicalHistory = await unitOfWork.Context.MedicalHistories
            .Include(m => m.Diagnoses)
            .Include(m => m.Exams)
            .Include(m => m.Prescriptions)
            .SingleAsync(x => x.Id == request.Id)
            ?? throw new InvalidOperationException($"Medical history with ID {request.Id} not found");

        medicalHistory = editMapper.ToEntity(request, medicalHistory);

        Models.Diagnosis? existingDiagnosis = medicalHistory.Diagnoses.FirstOrDefault(d => !d.IsDeleted );
        existingDiagnosis = diagnosisEditMapper.ToEntity(request.Diagnosis, existingDiagnosis);

        Models.Exam existingExam = medicalHistory.Exams.FirstOrDefault(e => !e.IsDeleted );
        existingExam = examEditMapper.ToEntity(request.Exam, existingExam);

        Models.Prescription prescription = medicalHistory.Prescriptions.FirstOrDefault(p => !p.IsDeleted );
        prescription = prescriptionEditMapper.ToEntity(request.Prescription, prescription);

        medicalHistory.Diagnoses.Add(existingDiagnosis);
        medicalHistory.Exams.Add(existingExam);
        medicalHistory.Prescriptions.Add(prescription);

        unitOfWork.Context.MedicalHistories.Update(medicalHistory);
        await unitOfWork.CommitAsync();

        var response = getMapper.ToDTO(medicalHistory);

        var cacheKey = string.Format(MEDICAL_HISTORY_CACHE_KEY, medicalHistory.Id);
        cache.Set(cacheKey, response, TimeSpan.FromMinutes(15));

        InvalidateListCaches();
        InvalidatePatientCaches(medicalHistory.PatientId, medicalHistory.Document);

        return response;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var medicalHistory = await unitOfWork.Context.MedicalHistories
            .Where(m => m.Id == id)
            .Select(m => new { m.PatientId, m.Document })
            .FirstOrDefaultAsync();

        if (medicalHistory == null) return false;

        int rows = await unitOfWork.Context.MedicalHistories.Where(_ => _.Id == id)
            .ExecuteUpdateAsync(_ =>
                _.SetProperty(mh => mh.IsDeleted, true)
                .SetProperty(mh => mh.UpdatedAt, DateTime.UtcNow));

        if (rows > 0)
        {
            InvalidateMedicalHistoryCache(id);
            InvalidateListCaches();
            InvalidatePatientCaches(medicalHistory.PatientId, medicalHistory.Document);
        }

        return rows > 0;
    }

    public async Task<Shared.DTOs.MedicalHistory.Get.Response> GetAsync(Guid id)
    {
        var cacheKey = string.Format(MEDICAL_HISTORY_CACHE_KEY, id);

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var medicalHistory = await unitOfWork.Context.MedicalHistories
                .Include(m => m.Diagnoses.Where(d => !d.IsDeleted))
                .Include(m => m.Exams.Where(e => !e.IsDeleted))
                .Include(m => m.Prescriptions.Where(p => !p.IsDeleted))
                .AsNoTracking()
                .SingleAsync(x => x.Id == id && !x.IsDeleted);

            return getMapper.ToDTO(medicalHistory);
        }, TimeSpan.FromMinutes(15));
    }

    public async Task<PaginationResponse<Shared.DTOs.MedicalHistory.Get.Response>> GetAsync(Shared.DTOs.MedicalHistory.Get.Request request, int page, int pageSize)
    {
        string json = request is null ? "{}" : System.Text.Json.JsonSerializer.Serialize(request);
        var cacheKey = string.Format(MEDICAL_HISTORIES_FILTERED_CACHE_KEY, json, page, pageSize);

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            IQueryable<Models.MedicalHistory> query = unitOfWork.Context.MedicalHistories
                .Include(m => m.Diagnoses.Where(d => !d.IsDeleted))
                .Include(m => m.Exams.Where(e => !e.IsDeleted))
                .Include(m => m.Prescriptions.Where(p => !p.IsDeleted))
                .AsNoTracking()
                .Where(_ => !_.IsDeleted);

            query = request switch
            {
                null => query,
                _ => query
                    .Where(m => !request.PatientId.HasValue || m.PatientId == request.PatientId.Value)
                    .Where(m => string.IsNullOrWhiteSpace(request.PatientDocument) || m.Document.Contains(request.PatientDocument))
                    .Where(m => !request.FromDate.HasValue || m.CreatedAt >= request.FromDate.Value)
                    .Where(m => !request.ToDate.HasValue || m.CreatedAt <= request.ToDate.Value)
            };

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            IEnumerable<Shared.DTOs.MedicalHistory.Get.Response> items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(medicalHistory => getMapper.ToDTO(medicalHistory))
                .ToListAsync();

            PaginationResponse pagination = new(page, pageSize, totalItems, totalPages);

            return new PaginationResponse<Shared.DTOs.MedicalHistory.Get.Response>(items, pagination);
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<Shared.DTOs.MedicalHistory.Get.Response?> GetByPatientAsync(Guid patientId)
    {
        var cacheKey = string.Format(PATIENT_MEDICAL_HISTORY_CACHE_KEY, patientId);

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var medicalHistory = await unitOfWork.Context.MedicalHistories
                .Include(m => m.Diagnoses.Where(d => !d.IsDeleted))
                .Include(m => m.Exams.Where(e => !e.IsDeleted))
                .Include(m => m.Prescriptions.Where(p => !p.IsDeleted))
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PatientId == patientId && !x.IsDeleted);

            return medicalHistory != null ? getMapper.ToDTO(medicalHistory) : null;
        }, TimeSpan.FromMinutes(15));
    }

    public async Task<Shared.DTOs.MedicalHistory.Get.Response?> GetByPatientDocumentAsync(string patientDocument)
    {
        var cacheKey = string.Format(PATIENT_DOC_MEDICAL_HISTORY_CACHE_KEY, patientDocument);

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var medicalHistory = await unitOfWork.Context.MedicalHistories
                .Include(m => m.Diagnoses.Where(d => !d.IsDeleted))
                .Include(m => m.Exams.Where(e => !e.IsDeleted))
                .Include(m => m.Prescriptions.Where(p => !p.IsDeleted))
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Document == patientDocument && !x.IsDeleted);

            return medicalHistory != null ? getMapper.ToDTO(medicalHistory) : null;
        }, TimeSpan.FromMinutes(15));
    }

    public async Task<bool> ExistsAsync(Expression<Func<Models.MedicalHistory, bool>> expression)
        => await unitOfWork.Context.MedicalHistories.AnyAsync(expression);

    private void InvalidateMedicalHistoryCache(Guid medicalHistoryId)
    {
        var cacheKey = string.Format(MEDICAL_HISTORY_CACHE_KEY, medicalHistoryId);
        cache.Remove(cacheKey);
    }

    private void InvalidatePatientCaches(Guid patientId, string patientDocument)
    {
        var patientCacheKey = string.Format(PATIENT_MEDICAL_HISTORY_CACHE_KEY, patientId);
        var patientDocCacheKey = string.Format(PATIENT_DOC_MEDICAL_HISTORY_CACHE_KEY, patientDocument);
        cache.Remove(patientCacheKey);
        cache.Remove(patientDocCacheKey);
    }

    private void InvalidateListCaches()
    {
        for (int page = 1; page <= 10; page++)
        {
            for (int pageSize = 10; pageSize <= 50; pageSize += 10)
            {
                var pageKey = string.Format(MEDICAL_HISTORIES_PAGE_CACHE_KEY, page, pageSize);
                cache.Remove(pageKey);
            }
        }
    }
}