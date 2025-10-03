using AuthService.API.Mapper.User;
using AuthService.API.Mapper.Auth;
using Shared.Mapper;

namespace AuthService.API.Extensions;

public static class MapperExtensions
{
    public static IServiceCollection AddMappers(this IServiceCollection services)
    {
        services.AddScoped<IAddMapper<Shared.DTOs.Users.Add.Request, Models.User>, AddMapper>();
        services.AddScoped<IEditMapper<Shared.DTOs.Users.Edit.Request, Models.User>, EditMapper>();
        services.AddScoped<IGetMapper<Models.User, Shared.DTOs.Users.Get.Response>, GetMapper>();

        services.AddScoped<IAddMapper<Shared.DTOs.Auth.Register, Models.User>, RegisterMapper>();

        return services;
    }
}