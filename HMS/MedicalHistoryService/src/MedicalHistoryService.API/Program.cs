using Microsoft.EntityFrameworkCore;
using MedicalHistoryService.API.Data;
using MedicalHistoryService.API.Extensions;
using MedicalHistoryService.API.Services;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["HttpSettings:Url"]
           ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
           ?? "http://+:5202";
builder.WebHost.UseUrls(urls);

builder.Services.AddOpenApi();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddApiExplorer();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
    );

builder.Services.AddApplicationServices();
builder.Services.AddMappers();
builder.Services.AddCacheService();

var rabbitConnectionString = builder.Configuration["MessageBroker:RabbitMQ"] ?? "amqp://localhost:5672";

try
{
    builder.Services.AddRabbitMQMessaging(rabbitConnectionString);
    builder.Services.AddHostedService<MedicalHistoryMessageConsumer>();
    builder.Services.AddHostedService<PatientCreatedMessageConsumer>();
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to register RabbitMQ services: {ex.Message}");
}

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await context.Database.MigrateAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapEndpoints();

app.Run();