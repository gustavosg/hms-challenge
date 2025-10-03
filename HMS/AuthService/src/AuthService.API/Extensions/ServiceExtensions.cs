using AuthService.API.Data;

using Shared.Extensions;

using Services = AuthService.API.Services;

namespace AuthService.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddUnitOfWork<ApplicationDbContext>();
        services.AddScoped<Services.Interfaces.IAuthService, Services.Implementations.AuthService>();
        services.AddScoped<Services.Interfaces.IUserService, Services.Implementations.UserService>();
        
        return services;
    }
}
