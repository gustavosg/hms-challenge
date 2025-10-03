using AuthService.API.Data;
using AuthService.API.Extensions;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Bind Kestrel URLs from configuration or env (works locally and in Docker)
var urls = builder.Configuration["HttpSettings:Url"] 
           ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") 
           ?? "http://+:5002";
builder.WebHost.UseUrls(urls);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddApiExplorer();

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
    );

// Register application services using extension method
builder.Services.AddApplicationServices();

// Register AuthService mappers explicitly (no reflection)
builder.Services.AddMappers();

// Register cache service
builder.Services.AddCacheService();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map Health Check endpoint
app.MapHealthChecks("/health");

app.MapEndpoints();

app.Run();

