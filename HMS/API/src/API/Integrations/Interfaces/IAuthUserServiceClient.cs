using Refit;
using Shared.DTOs;
using Shared.DTOs.Users.Get;

using AuthDTO = Shared.DTOs.Auth;
using UserDTO = Shared.DTOs.Users;

namespace API.Integrations.Interfaces;

public interface IAuthUserServiceClient
{
    // Auth endpoints
    [Post("/api/auth/login")]
    Task<object> LoginAsync([Body] AuthDTO.Login request);

    [Post("/api/auth/register")]
    Task<Guid> RegisterAsync([Body] AuthDTO.Register request);

    // Users endpoints
    [Get("/api/users")]
    Task<PaginationResponse<UserDTO.Get.Response>> GetUsersAsync([Query] Request request, [Query] int page, [Query] int pageSize);

    [Get("/api/users/{id}")]
    Task<UserDTO.Get.Response> GetUserByIdAsync(Guid id);

    [Post("/api/users")]
    Task<Guid> AddUserAsync([Body] UserDTO.Add.Request request);

    [Put("/api/users/{id}")]
    Task<UserDTO.Get.Response> EditUserAsync(Guid id, [Body] UserDTO.Edit.Request request);

    [Delete("/api/users/{id}")]
    Task DeleteUserAsync(Guid id);
}
