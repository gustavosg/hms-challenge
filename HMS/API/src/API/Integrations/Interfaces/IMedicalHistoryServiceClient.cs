using Refit;
using Shared.DTOs;
using DTO = Shared.DTOs.MedicalHistory;

namespace API.Integrations.Interfaces;

public interface IMedicalHistoryServiceClient
{
    [Post("/api/medical-histories")]
    Task<Guid> AddMedicalHistoryAsync(DTO.Add.Request request);
    
    [Get("/api/medical-histories")]
    Task<PaginationResponse<DTO.Get.Response>> GetMedicalHistoriesAsync([Query] DTO.Get.Request request, [Query] int page, [Query] int pageSize);
    
    [Get("/api/medical-histories/{id}")]
    Task<DTO.Get.Response> GetMedicalHistoryByIdAsync(Guid id);
    
    [Get("/api/medical-histories/patient/{patientId}")]
    Task<DTO.Get.Response> GetMedicalHistoryByPatientIdAsync(Guid patientId);
    
    [Get("/api/medical-histories/patient/document/{document}")]
    Task<DTO.Get.Response> GetMedicalHistoryByPatientDocumentAsync(string document);
    
    [Put("/api/medical-histories/{id}")]
    Task<DTO.Get.Response> EditMedicalHistoryAsync(Guid id, DTO.Edit.Request request);
    
    [Delete("/api/medical-histories/{id}")]
    Task DeleteMedicalHistoryAsync(Guid id);
}