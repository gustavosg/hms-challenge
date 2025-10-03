using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using PatientsService.API.Data;
using PatientsService.API.Models;
using PatientsService.API.Services.Interfaces;

using Shared.DTOs;
using Shared.Infra.UnitOfWork;
using Shared.Mapper;
using Shared.Services.Cache;
using Shared.Services.Messaging;

namespace PatientsService.API.Services.Implementations;

public class PatientService(
    IAddMapper<Shared.DTOs.Patient.Add.Request, Models.Patient> addMapper,
    IEditMapper<Shared.DTOs.Patient.Edit.Request, Models.Patient> editMapper,
    IGetMapper<Models.Patient, Shared.DTOs.Patient.Get.Response> getMapper,
    IUnitOfWork<ApplicationDbContext> unitOfWork,
    ICacheService cache,
    IMessageBrokerService messageBroker)
    : IPatientService
{
    private const string PATIENT_CACHE_KEY = "patient:{0}";
    private const string PATIENTS_LIST_CACHE_KEY = "patients:all";
    private const string PATIENTS_PAGE_CACHE_KEY = "patients:page:{0}:{1}";
    private const string PATIENTS_FILTERED_CACHE_KEY = "patients:filtered:{0}:page:{1}:{2}";

    public async Task<Guid> AddAsync(Shared.DTOs.Patient.Add.Request request)
    {
        Patient patient = addMapper.ToEntity(request);
        
        await unitOfWork.Context.Patients.AddAsync(patient);
        await unitOfWork.CommitAsync();

        // Enviar mensagem para criar hist�rico m�dico
        var createMedicalHistoryMessage = new
        {
            PatientId = patient.Id,
            Document = patient.Document,
            PatientName = patient.Name,
            CreatedAt = DateTime.UtcNow
        };

        await messageBroker.PublishAsync("patient.created", createMedicalHistoryMessage);

        InvalidateListCaches();

        return patient.Id;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        int rows = await unitOfWork.Context.Patients.Where(_ => _.Id == id)
            .ExecuteUpdateAsync(_ =>
                _.SetProperty(patient => patient.IsDeleted, true)
                .SetProperty(patient => patient.UpdatedAt, DateTime.UtcNow));

        if (rows > 0)
        {
            InvalidatePatientCache(id);
            InvalidateListCaches();
        }

        return rows > 0;
    }

    public async Task<Shared.DTOs.Patient.Get.Response> EditAsync(Shared.DTOs.Patient.Edit.Request request)
    {
        Patient patient = await unitOfWork.Context.Patients.AsNoTracking().SingleAsync(x => x.Id == request.Id)
            ?? throw new InvalidOperationException($"Patient with ID {request.Id} not found");

        patient = editMapper.ToEntity(request, patient);

        unitOfWork.Context.Patients.Update(patient);
        await unitOfWork.CommitAsync();
        
        var response = getMapper.ToDTO(patient);

        var patientCacheKey = string.Format(PATIENT_CACHE_KEY, patient.Id);
        cache.Set(patientCacheKey, response, TimeSpan.FromMinutes(15));

        InvalidateListCaches();

        return response;
    }

    public async Task<Shared.DTOs.Patient.Get.Response> GetAsync(Guid id)
    {
        var cacheKey = string.Format(PATIENT_CACHE_KEY, id);

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var patient = await unitOfWork.Context.Patients
                .AsNoTracking()
                .SingleAsync(x => x.Id == id && !x.IsDeleted);

            return getMapper.ToDTO(patient);
        }, TimeSpan.FromMinutes(15));
    }

    public async Task<PaginationResponse<Shared.DTOs.Patient.Get.Response>> GetAsync(Shared.DTOs.Patient.Get.Request request, int page, int pageSize)
    {
        string json = request is null ? "{}" : System.Text.Json.JsonSerializer.Serialize(request);
        var cacheKey = string.Format(PATIENTS_FILTERED_CACHE_KEY, json, page, pageSize);

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            IQueryable<Models.Patient> query = unitOfWork.Context.Patients
                .AsNoTracking()
                .Where(_ => !_.IsDeleted);

            query = request switch
            {
                null => query,
                _ => query
                    .Where(p => string.IsNullOrWhiteSpace(request.Name) || p.Name.Contains(request.Name))
                    .Where(p => string.IsNullOrWhiteSpace(request.Document) || p.Document.Contains(request.Document))
                    .Where(p => string.IsNullOrWhiteSpace(request.Email) || p.Email.Contains(request.Email))
                    .Where(p => !request.BirthDateMin.HasValue || p.BirthDate >= request.BirthDateMin.Value)
                    .Where(p => !request.BirthDateMax.HasValue || p.BirthDate <= request.BirthDateMax.Value)
            };

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            IEnumerable<Shared.DTOs.Patient.Get.Response> items = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(patient => getMapper.ToDTO(patient))
                .ToListAsync();

            PaginationResponse pagination = new(page, pageSize, totalItems, totalPages);

            return new PaginationResponse<Shared.DTOs.Patient.Get.Response>(items, pagination);
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<bool> ExistsAsync(Expression<Func<Models.Patient, bool>> expression)
        => await unitOfWork.Context.Patients.AnyAsync(expression);

    private void InvalidatePatientCache(Guid patientId)
    {
        var patientCacheKey = string.Format(PATIENT_CACHE_KEY, patientId);
        cache.Remove(patientCacheKey);
    }

    private void InvalidateListCaches()
    {
        cache.Remove(PATIENTS_LIST_CACHE_KEY);

        for (int page = 1; page <= 10; page++)
        {
            for (int pageSize = 10; pageSize <= 50; pageSize += 10)
            {
                var pageKey = string.Format(PATIENTS_PAGE_CACHE_KEY, page, pageSize);
                cache.Remove(pageKey);
            }
        }
    }
}