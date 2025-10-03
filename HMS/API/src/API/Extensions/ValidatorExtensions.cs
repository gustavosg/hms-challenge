using System.Reflection;

using FluentValidation;

using Shared.DTOs.Users.Add;

namespace API.Extensions;

public static class ValidatorExtensions
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(Shared.DTOs.Users.Add.RequestValidator).Assembly);

        return services;
    }
}
