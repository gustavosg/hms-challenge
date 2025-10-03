using Microsoft.EntityFrameworkCore;
using PatientsService.API.Data;
using PatientsService.API.Extensions;
using PatientsService.API.Services.Implementations;
using PatientsService.API.Services.Interfaces;
using Shared.Extensions;
using Shared.Infra.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var urls = builder.Configuration["HttpSettings:Url"]
           ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
           ?? "http://+:5102";
builder.WebHost.UseUrls(urls);

// Database - PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Unit of Work
builder.Services.AddScoped<IUnitOfWork<ApplicationDbContext>, UnitOfWork<ApplicationDbContext>>();

// Services
builder.Services.AddScoped<IPatientService, PatientsService.API.Services.Implementations.PatientService>();

// Patient Mappers (explicit registration, no reflection)
builder.Services.AddMappers();

// Shared services
builder.Services.AddCacheService();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();

// RabbitMQ Messaging
var rabbitConnectionString = builder.Configuration["MessageBroker:RabbitMQ"] ?? "amqp://localhost:5672";
builder.Services.AddRabbitMQMessaging(rabbitConnectionString);

builder.Services.AddAuthorization();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Auto-apply migrations with retry logic
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var maxRetries = 5;
var delay = TimeSpan.FromSeconds(5);

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        await context.Database.MigrateAsync();
        break;
    }
    catch (Exception ex) when (i < maxRetries - 1)
    {
        Console.WriteLine($"Migration attempt {i + 1} failed: {ex.Message}. Retrying in {delay.TotalSeconds} seconds...");
        await Task.Delay(delay);
    }
}

// Start RabbitMQ listener with retry logic
try
{
    var messageBroker = app.Services.GetRequiredService<Shared.Services.Messaging.IMessageBrokerService>();
    messageBroker.StartListening();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Failed to start RabbitMQ listener. The service will continue but messaging features may not work.");
}

app.UseAuthentication();
app.UseAuthorization();

// Map Health Check endpoint
app.MapHealthChecks("/health");

// Map endpoints
app.MapEndpoints();

app.Run();