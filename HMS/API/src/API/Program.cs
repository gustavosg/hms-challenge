using API.Extensions;
using API.Services.Interfaces;
using API.Services.Implementations;
using Shared.Extensions;
using Shared.Infra.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidators();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddRefitClient(builder.Configuration);
builder.Services.AddTransient<BearerTokenPropagationHandler>();

builder.Services.AddHttpClient<ExternalExamService>();
builder.Services.AddScoped<IExternalExamService, ExternalExamService>();

builder.Services.AddCacheService();
builder.Services.AddAuthorization();
builder.Services.AddApiExplorer();

var rabbitConnectionString = builder.Configuration["MessageBroker:RabbitMQ"] ?? "amqp://localhost:5672";
try
{
    builder.Services.AddRabbitMQMessaging(rabbitConnectionString);
    Console.WriteLine($"API Gateway: RabbitMQ configured with: {rabbitConnectionString}");
}
catch (Exception ex)
{
    Console.WriteLine($"API Gateway: Failed to configure RabbitMQ: {ex.Message}");
    Console.WriteLine("Continuing without RabbitMQ messaging...");
}

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

try
{
    using var scope = app.Services.CreateScope();
    var messageBroker = scope.ServiceProvider.GetService<Shared.Services.Messaging.IMessageBrokerService>();
    if (messageBroker != null)
    {
        messageBroker.StartListening();
        Console.WriteLine("API Gateway: RabbitMQ listener started for medical history responses");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"API Gateway: Failed to start RabbitMQ listener: {ex.Message}");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapEndpoints();

Console.WriteLine("API Gateway started successfully");
app.Run();
