using Shared.DTOs.Auth;

namespace AuthService.API.Services.Interfaces;

public interface IAuthService
{
    Task<string> Login(Login request);
    Task<Guid> Register(Register request);
}
