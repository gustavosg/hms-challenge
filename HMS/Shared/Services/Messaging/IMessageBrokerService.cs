using Shared.Messages;

namespace Shared.Services.Messaging;

public interface IMessageBrokerService
{
    Task<GetMedicalHistoryResponse> RequestMedicalHistoryAsync(GetMedicalHistoryRequest request, TimeSpan timeout);
    Task PublishMedicalHistoryResponseAsync(GetMedicalHistoryResponse response);
    Task PublishAsync<T>(string routingKey, T message);
    Task ConsumeAsync(string queueName, Func<string, Task> handler, CancellationToken cancellationToken = default);
    void StartListening();
    void StopListening();
}