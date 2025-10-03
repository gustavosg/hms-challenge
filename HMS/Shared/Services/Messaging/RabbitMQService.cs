using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messages;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Reflection;

namespace Shared.Services.Messaging;

public class RabbitMQService : IMessageBrokerService, IDisposable
{
    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly string requestQueue = "medical-history-requests";
    private readonly string responseQueue = "medical-history-responses";
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<GetMedicalHistoryResponse>> pendingRequests = new();

    public RabbitMQService(string connectionString)
    {
        try
        {
            var factory = new ConnectionFactory();
            
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                factory.Uri = uri;
            }
            else
            {
                factory.HostName = "localhost";
                factory.Port = 5672;
                factory.UserName = "guest";
                factory.Password = "guest";
            }

            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
            factory.RequestedHeartbeat = TimeSpan.FromSeconds(60);
            factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(30);

            var createConnectionMethod = typeof(ConnectionFactory).GetMethod("CreateConnection", new Type[] { });
            if (createConnectionMethod == null)
            {
                var methods = typeof(ConnectionFactory).GetMethods().Where(m => m.Name == "CreateConnection").ToArray();
                createConnectionMethod = methods.FirstOrDefault(m => m.GetParameters().Length == 0);
            }

            if (createConnectionMethod != null)
            {
                connection = (IConnection)createConnectionMethod.Invoke(factory, null)!;
            }
            else
            {
                throw new InvalidOperationException("Could not find CreateConnection method on ConnectionFactory");
            }

            channel = connection.CreateModel();

            channel.QueueDeclare(requestQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(responseQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to connect to RabbitMQ at '{connectionString}': {ex.Message}", ex);
        }
    }

    public async Task<GetMedicalHistoryResponse?> RequestMedicalHistoryAsync(GetMedicalHistoryRequest request, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<GetMedicalHistoryResponse>();
        pendingRequests[request.CorrelationId] = tcs;

        try
        {
            var json = JsonSerializer.Serialize(request);
            var body = Encoding.UTF8.GetBytes(json);

            var props = channel.CreateBasicProperties();
            props.CorrelationId = request.CorrelationId.ToString();
            props.ReplyTo = responseQueue;
            props.DeliveryMode = 2;

            channel.BasicPublish(
                exchange: "",
                routingKey: requestQueue,
                basicProperties: props,
                body: body);

            using var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() => tcs.TrySetCanceled());

            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            return new GetMedicalHistoryResponse(request.CorrelationId, false, null, "Request timeout");
        }
        catch (Exception ex)
        {
            return new GetMedicalHistoryResponse(request.CorrelationId, false, null, $"Request failed: {ex.Message}");
        }
        finally
        {
            pendingRequests.TryRemove(request.CorrelationId, out _);
        }
    }

    public Task PublishMedicalHistoryResponseAsync(GetMedicalHistoryResponse response)
    {
        try
        {
            var json = JsonSerializer.Serialize(response);
            var body = Encoding.UTF8.GetBytes(json);

            var props = channel.CreateBasicProperties();
            props.CorrelationId = response.CorrelationId.ToString();
            props.DeliveryMode = 2;

            channel.BasicPublish(
                exchange: "",
                routingKey: responseQueue,
                basicProperties: props,
                body: body);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    public Task PublishAsync<T>(string routingKey, T message)
    {
        try
        {
            channel.QueueDeclare(routingKey, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = channel.CreateBasicProperties();
            props.DeliveryMode = 2;

            channel.BasicPublish(
                exchange: "",
                routingKey: routingKey,
                basicProperties: props,
                body: body);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    public Task ConsumeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken cancellationToken = default)
    {
        try
        {
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                    {
                        await handler(message);
                    }

                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception)
                {
                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            channel.BasicConsume(queueName, autoAck: false, consumer);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    public Task ConsumeAsync(string queueName, Func<string, Task> handler, CancellationToken cancellationToken = default)
    {
        try
        {
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);

                    await handler(json);

                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception)
                {
                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            channel.BasicConsume(queueName, autoAck: false, consumer);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    public void StartListening()
    {
        try
        {
            var responseConsumer = new EventingBasicConsumer(channel);
            responseConsumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var response = JsonSerializer.Deserialize<GetMedicalHistoryResponse>(json);

                    if (response != null && pendingRequests.TryRemove(response.CorrelationId, out var tcs))
                    {
                        tcs.SetResult(response);
                    }

                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception)
                {
                    channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            channel.BasicConsume(responseQueue, autoAck: false, responseConsumer);
        }
        catch (Exception)
        {
        }
    }

    public void StopListening()
    {
    }

    public void Dispose()
    {
        try
        {
            channel?.Close();
            channel?.Dispose();
            connection?.Close();
            connection?.Dispose();
        }
        catch (Exception)
        {
        }
    }
}