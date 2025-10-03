using Microsoft.Extensions.DependencyInjection;
using Shared.Services.Cache;

namespace Shared.Extensions;

public static class CacheExtensions
{
    public static IServiceCollection AddCacheService(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<ICacheService, CacheService>();
        
        return services;
    }
}