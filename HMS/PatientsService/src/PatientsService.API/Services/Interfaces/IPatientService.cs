using System.Linq.Expressions;
using Shared.DTOs;

namespace PatientsService.API.Services.Interfaces;

public interface IPatientService
{
    Task<Guid> AddAsync(Shared.DTOs.Patient.Add.Request request);
    Task<Shared.DTOs.Patient.Get.Response> GetAsync(Guid id);
    Task<PaginationResponse<Shared.DTOs.Patient.Get.Response>> GetAsync(Shared.DTOs.Patient.Get.Request request, int page, int pageSize);
    Task<Shared.DTOs.Patient.Get.Response> EditAsync(Shared.DTOs.Patient.Edit.Request request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Expression<Func<Models.Patient, bool>> expression);
}