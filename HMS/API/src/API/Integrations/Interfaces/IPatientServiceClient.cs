using Refit;

using Shared.DTOs;

using DTO = Shared.DTOs.Patient;

namespace API.Integrations.Interfaces;

public interface IPatientServiceClient
{
    [Post("/api/patients")]
    Task<Guid> AddPatientAsync(DTO.Add.Request request);
    
    [Get("/api/patients")]
    Task<PaginationResponse<DTO.Get.Response>> GetPatientsAsync([Query] DTO.Get.Request request, [Query] int page, [Query] int pageSize);
    
    [Get("/api/patients/{id}")]
    Task<DTO.Get.Response> GetPatientByIdAsync(Guid id);
    
    [Put("/api/patients/{id}")]
    Task<DTO.Get.Response> EditPatientAsync(Guid id, DTO.Edit.Request request);
    
    [Delete("/api/patients/{id}")]
    Task DeletePatientAsync(Guid id);
}
