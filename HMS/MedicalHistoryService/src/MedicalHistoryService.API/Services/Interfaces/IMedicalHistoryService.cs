using System.Linq.Expressions;
using Shared.DTOs;

namespace MedicalHistoryService.API.Services.Interfaces;

public interface IMedicalHistoryService
{
    Task<Guid> AddAsync(Shared.DTOs.MedicalHistory.Add.Request request);
    Task<Shared.DTOs.MedicalHistory.Get.Response> GetAsync(Guid id);
    Task<PaginationResponse<Shared.DTOs.MedicalHistory.Get.Response>> GetAsync(Shared.DTOs.MedicalHistory.Get.Request request, int page, int pageSize);
    Task<Shared.DTOs.MedicalHistory.Get.Response> EditAsync(Shared.DTOs.MedicalHistory.Edit.Request request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Expression<Func<Models.MedicalHistory, bool>> expression);
    Task<Shared.DTOs.MedicalHistory.Get.Response?> GetByPatientAsync(Guid patientId);
    Task<Shared.DTOs.MedicalHistory.Get.Response?> GetByPatientDocumentAsync(string patientDocument);
}