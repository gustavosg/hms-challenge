using MedicalHistoryService.API.Services.Interfaces;
using Shared.Services.Messaging;
using System.Text.Json;

namespace MedicalHistoryService.API.Services;

public class PatientCreatedMessageConsumer : BackgroundService
{
    private readonly ILogger<PatientCreatedMessageConsumer> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;

    public PatientCreatedMessageConsumer(
        ILogger<PatientCreatedMessageConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        this.configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PatientCreatedMessageConsumer: Starting...");

        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var messageBroker = scope.ServiceProvider.GetService<IMessageBrokerService>();
                
                if (messageBroker != null)
                {
                    logger.LogInformation("PatientCreatedMessageConsumer: Message broker available, starting to consume...");
                    
                    await messageBroker.ConsumeAsync("patient.created", async (string messageJson) =>
                    {
                        try
                        {
                            var message = JsonSerializer.Deserialize<PatientCreatedMessage>(messageJson);
                            if (message == null)
                            {
                                logger.LogWarning("Received null patient created message");
                                return;
                            }
                            
                            logger.LogInformation("Received patient created message for patient: {PatientId} - {PatientName}", 
                                message.PatientId, message.PatientName);

                            using var innerScope = serviceProvider.CreateScope();
                            var medicalHistoryService = innerScope.ServiceProvider.GetRequiredService<IMedicalHistoryService>();

                            bool exists = await medicalHistoryService.ExistsAsync(mh => mh.PatientId == message.PatientId && !mh.IsDeleted);
                            
                            if (!exists)
                            {
                                var createRequest = new Shared.DTOs.MedicalHistory.Add.Request(
                                    PatientId: message.PatientId,
                                    Document: message.Document,
                                    Notes: $"Histórico médico criado automaticamente para paciente {message.PatientName}",
                                    Diagnosis: null,
                                    Exam: null,
                                    Prescription: null
                                );

                                var medicalHistoryId = await medicalHistoryService.AddAsync(createRequest);
                                
                                logger.LogInformation("Medical history created for patient {PatientId} ({PatientName}) with ID {MedicalHistoryId}", 
                                    message.PatientId, message.PatientName, medicalHistoryId);
                            }
                            else
                            {
                                logger.LogInformation("Medical history already exists for patient {PatientId} ({PatientName})", 
                                    message.PatientId, message.PatientName);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error processing patient created message");
                            throw;
                        }
                    }, stoppingToken);

                    logger.LogInformation("PatientCreatedMessageConsumer: Successfully started consuming 'patient.created' queue");
                    
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                    
                    break;
                }
                else
                {
                    logger.LogWarning("PatientCreatedMessageConsumer: Message broker not available, retrying in 30 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PatientCreatedMessageConsumer: Error in ExecuteAsync");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        
        logger.LogInformation("PatientCreatedMessageConsumer: Stopped");
    }

    private class PatientCreatedMessage
    {
        public Guid PatientId { get; set; }
        public string Document { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}