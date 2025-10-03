using Microsoft.Extensions.DependencyInjection;
using Shared.Services.Messaging;
using Shared.Messages;

namespace Shared.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMQMessaging(this IServiceCollection services, string connectionString)
    {
        // Registrar como factory para evitar falhas na inicializa��o
        services.AddSingleton<IMessageBrokerService>(provider => 
        {
            try
            {
                return new RabbitMQService(connectionString);
            }
            catch (Exception ex)
            {
                // Log the error but return a null service that doesn't break DI
                Console.WriteLine($"Failed to create RabbitMQ service: {ex.Message}");
                return new NullMessageBrokerService();
            }
        });
        
        return services;
    }
}

// Implementa��o nula para quando RabbitMQ n�o estiver dispon�vel
public class NullMessageBrokerService : IMessageBrokerService
{
    public Task<GetMedicalHistoryResponse> RequestMedicalHistoryAsync(GetMedicalHistoryRequest request, TimeSpan timeout)
    {
        return Task.FromResult(new GetMedicalHistoryResponse(request.CorrelationId, false, null, "RabbitMQ service not available"));
    }

    public Task PublishMedicalHistoryResponseAsync(GetMedicalHistoryResponse response)
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(string routingKey, T message)
    {
        return Task.CompletedTask;
    }

    public Task ConsumeAsync(string queueName, Func<string, Task> handler, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void StartListening()
    {
        // No-op
    }

    public void StopListening()
    {
        // No-op
    }
}