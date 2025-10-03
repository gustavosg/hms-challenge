using API.Integrations.Interfaces;

using Polly;
using Polly.Extensions.Http;

using Refit;

using Shared.Infra.Http;

namespace API.Extensions;

public static class RefitExtensions
{
    public static void AddRefitClient(this IServiceCollection services, IConfiguration configuration)
    {
        // AuthService configuration
        var authServiceBaseUrl =
            configuration["Services:AuthUserService:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("AUTH_USER_SERVICE_BASEURL")
            ?? "http://localhost:5002";

        services.AddRefitClient<IAuthUserServiceClient>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(authServiceBaseUrl);
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<BearerTokenPropagationHandler>() 
            .AddPolicyHandler(GetRetryPolicy());

        // PatientService configuration
        var patientServiceBaseUrl =
            configuration["Services:PatientService:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("PATIENT_SERVICE_BASEURL")
            ?? "http://localhost:5003";

        services.AddRefitClient<IPatientServiceClient>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(patientServiceBaseUrl);
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<BearerTokenPropagationHandler>()
            .AddPolicyHandler(GetRetryPolicy());

        // MedicalHistoryService configuration
        var medicalHistoryServiceBaseUrl =
            configuration["Services:MedicalHistoryService:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("MEDICAL_HISTORY_SERVICE_BASEURL")
            ?? "http://localhost:5004";

        services.AddRefitClient<IMedicalHistoryServiceClient>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(medicalHistoryServiceBaseUrl);
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<BearerTokenPropagationHandler>()
            .AddPolicyHandler(GetRetryPolicy());
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }
}
