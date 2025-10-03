using System.Linq.Expressions;

using Shared.DTOs;
using Shared.DTOs.Users.Get;

using UserDTO = Shared.DTOs.Users;

namespace AuthService.API.Services.Interfaces;

public interface IUserService
{
    Task<Guid> AddAsync(UserDTO.Add.Request request);
    Task<UserDTO.Get.Response> EditAsync(UserDTO.Edit.Request request);
    Task<bool> DeleteAsync(Guid id);
    Task<PaginationResponse<Shared.DTOs.Users.Get.Response>> GetAsync(UserDTO.Get.Request request, int page, int pageSize);
    Task<bool> ExistsAsync(Expression<Func<Models.User, bool>> expression);
    Task<Response> GetAsync(Guid id);
}
