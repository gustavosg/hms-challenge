using MedicalHistoryService.API.Mapper.MedicalHistory;
using MedicalHistoryService.API.Mapper.Diagnosis;
using MedicalHistoryService.API.Mapper.Exam;
using MedicalHistoryService.API.Mapper.Prescription;
using Shared.Mapper;
using MedicalHistoryService.API.Models;
using DTOs = Shared.DTOs;

namespace MedicalHistoryService.API.Extensions;

public static class MapperExtensions
{
    public static IServiceCollection AddMappers(this IServiceCollection services)
    {
        // MedicalHistory mappers
        services.AddScoped<IAddMapper<Shared.DTOs.MedicalHistory.Add.Request, Models.MedicalHistory>, Mapper.MedicalHistory.AddMapper>();
        services.AddScoped<IEditMapper<Shared.DTOs.MedicalHistory.Edit.Request, Models.MedicalHistory>, Mapper.MedicalHistory.EditMapper>();
        services.AddScoped<IGetMapper<Models.MedicalHistory, Shared.DTOs.MedicalHistory.Get.Response>, Mapper.MedicalHistory.GetMapper>();

        // Diagnosis mappers
        services.AddScoped<IAddMapper<Shared.DTOs.MedicalHistory.Add.DiagnosisRequest, Models.Diagnosis>, Mapper.Diagnosis.AddMapper>();
        services.AddScoped<IEditMapper<Shared.DTOs.MedicalHistory.Edit.DiagnosisRequest, Models.Diagnosis>, Mapper.Diagnosis.EditMapper>();

        // Exam mappers
        services.AddScoped<IAddMapper<Shared.DTOs.MedicalHistory.Add.ExamRequest, Models.Exam>, Mapper.Exam.AddMapper>();
        services.AddScoped<IEditMapper<Shared.DTOs.MedicalHistory.Edit.ExamRequest, Models.Exam>, Mapper.Exam.EditMapper>();

        // Prescription mappers
        services.AddScoped<IAddMapper<Shared.DTOs.MedicalHistory.Add.PrescriptionRequest, Models.Prescription>, Mapper.Prescription.AddMapper>();
        services.AddScoped<IEditMapper<Shared.DTOs.MedicalHistory.Edit.PrescriptionRequest, Models.Prescription>, Mapper.Prescription.EditMapper>();

        return services;
    }
}


