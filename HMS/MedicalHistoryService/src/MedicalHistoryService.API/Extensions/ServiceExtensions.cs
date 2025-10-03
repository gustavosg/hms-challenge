using MedicalHistoryService.API.Data;
using MedicalHistoryService.API.Mapper.MedicalHistory;
using Shared.Extensions;
using Shared.Mapper;
using Services = MedicalHistoryService.API.Services;

namespace MedicalHistoryService.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddUnitOfWork<ApplicationDbContext>();
        services.AddScoped<Services.Interfaces.IMedicalHistoryService, Services.Implementations.MedicalHistoryService>();
        
        return services;
    }
}