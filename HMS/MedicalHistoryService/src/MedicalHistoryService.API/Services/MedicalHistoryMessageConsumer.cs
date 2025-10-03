using MedicalHistoryService.API.Services.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messages;
using Shared.Services.Messaging;
using System.Text;
using System.Text.Json;

namespace MedicalHistoryService.API.Services;

public class MedicalHistoryMessageConsumer : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<MedicalHistoryMessageConsumer> logger;
    private readonly IConfiguration configuration;
    private IConnection? connection;
    private IModel? channel;
    private readonly string requestQueue = "medical-history-requests";

    public MedicalHistoryMessageConsumer(
        IServiceProvider serviceProvider,
        ILogger<MedicalHistoryMessageConsumer> logger,
        IConfiguration configuration)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MedicalHistoryMessageConsumer: Starting...");

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await TryConnectToRabbitMQ())
                {
                    logger.LogInformation("MedicalHistoryMessageConsumer: Connected successfully");
                    await StartConsuming(stoppingToken);
                    break;
                }
                else
                {
                    logger.LogWarning("MedicalHistoryMessageConsumer: Failed to connect, retrying in 30 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MedicalHistoryMessageConsumer: Error in ExecuteAsync");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task<bool> TryConnectToRabbitMQ()
    {
        try
        {
            var connectionString = configuration["MessageBroker:RabbitMQ"] ?? "amqp://localhost:5672";
            logger.LogInformation("Attempting to connect to RabbitMQ at: {ConnectionString}", connectionString);

            var factory = new ConnectionFactory();
            factory.Uri = new Uri(connectionString);
            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(requestQueue, durable: true, exclusive: false, autoDelete: false);
            
            logger.LogInformation("RabbitMQ connection established successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to RabbitMQ");
            return false;
        }
    }

    private async Task StartConsuming(CancellationToken stoppingToken)
    {
        if (channel == null)
        {
            logger.LogError("Channel is null, cannot start consuming");
            return;
        }

        var consumer = new EventingBasicConsumer(channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                logger.LogInformation("MedicalHistoryMessageConsumer: Received message");
                
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var request = JsonSerializer.Deserialize<GetMedicalHistoryRequest>(json);

                if (request != null)
                {
                    logger.LogInformation("Processing medical history request for PatientId: {PatientId}", request.PatientId);

                    using var scope = serviceProvider.CreateScope();
                    var medicalHistoryService = scope.ServiceProvider.GetRequiredService<IMedicalHistoryService>();
                    var messageBroker = scope.ServiceProvider.GetRequiredService<IMessageBrokerService>();

                    try
                    {
                        var medicalHistory = await medicalHistoryService.GetByPatientAsync(request.PatientId);

                        var response = new GetMedicalHistoryResponse(
                            request.CorrelationId,
                            true,
                            medicalHistory,
                            null
                        );

                        await messageBroker.PublishMedicalHistoryResponseAsync(response);
                        logger.LogInformation("Medical history response sent for PatientId: {PatientId}", request.PatientId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing medical history request for PatientId: {PatientId}", request.PatientId);
                        
                        var errorResponse = new GetMedicalHistoryResponse(
                            request.CorrelationId,
                            false,
                            null,
                            ex.Message
                        );

                        await messageBroker.PublishMedicalHistoryResponseAsync(errorResponse);
                    }
                }

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in message consumer");
                channel?.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        channel.BasicConsume(requestQueue, autoAck: false, consumer);
        logger.LogInformation("MedicalHistoryMessageConsumer: Listening on queue '{Queue}'", requestQueue);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            
            if (connection?.IsOpen != true)
            {
                logger.LogWarning("RabbitMQ connection lost, will attempt to reconnect...");
                break;
            }
        }
    }

    public override void Dispose()
    {
        try
        {
            channel?.Close();
            channel?.Dispose();
            connection?.Close();
            connection?.Dispose();
            logger.LogInformation("MedicalHistoryMessageConsumer: Connections closed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disposing MedicalHistoryMessageConsumer");
        }
        
        base.Dispose();
    }
}