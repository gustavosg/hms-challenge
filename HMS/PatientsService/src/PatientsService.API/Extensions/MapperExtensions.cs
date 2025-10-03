using PatientsService.API.Mapper.Patient;
using Shared.DTOs.Patient.Get;
using Shared.Mapper;

namespace PatientsService.API.Extensions;

public static class MapperExtensions
{
    public static IServiceCollection AddMappers(this IServiceCollection services)
    {
        // Patient mappers
        services.AddScoped<IAddMapper<Shared.DTOs.Patient.Add.Request, Models.Patient>, AddMapper>();
        services.AddScoped<IEditMapper<Shared.DTOs.Patient.Edit.Request, Models.Patient>, EditMapper>();
        services.AddScoped<IGetMapper<Models.Patient, Response>, GetMapper>();

        return services;
    }
}