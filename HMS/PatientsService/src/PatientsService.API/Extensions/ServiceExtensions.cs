using PatientsService.API.Data;

using Shared.Extensions;

using Services = PatientsService.API.Services;

namespace PatientsService.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddUnitOfWork<ApplicationDbContext>();
        services.AddScoped<Services.Interfaces.IPatientService, Services.Implementations.PatientService>();
        
        return services;
    }
}
