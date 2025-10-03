using Microsoft.AspNetCore.Mvc;
using MedicalHistoryService.API.Services.Interfaces;
using Shared.DTOs;

namespace MedicalHistoryService.API.Extensions;

public static class EndpointExtensions
{
    private static class Routes
    {
        public const string MedicalHistories = "/api/medical-histories";
    }

    private static class Tags
    {
        public const string MedicalHistories = "Medical Histories";
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMedicalHistoriesEndpoints();
        return app;
    }

    private static IEndpointRouteBuilder MapMedicalHistoriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.MedicalHistories)
            .RequireAuthorization()
            .WithTags(Tags.MedicalHistories);

        group.MapGet("", async (
            [AsParameters] Shared.DTOs.MedicalHistory.Get.Request request,
            [FromServices] IMedicalHistoryService medicalHistoryService,
            int page = 1,
            int pageSize = 20) =>
        {
            try
            {
                var medicalHistories = await medicalHistoryService.GetAsync(request, page, pageSize);
                return Results.Ok(medicalHistories);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error retrieving medical histories: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IMedicalHistoryService medicalHistoryService) =>
        {
            try
            {
                var medicalHistory = await medicalHistoryService.GetAsync(id);
                return Results.Ok(medicalHistory);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound("Medical history not found");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error retrieving medical history: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/patient/{patientId:guid}", async (
            Guid patientId,
            [FromServices] IMedicalHistoryService medicalHistoryService) =>
        {
            try
            {
                var medicalHistory = await medicalHistoryService.GetByPatientAsync(patientId);
                if (medicalHistory == null)
                    return Results.NotFound("Medical history not found for this patient");

                return Results.Ok(medicalHistory);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error retrieving medical history: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/patient/document/{document}", async (
            string document,
            [FromServices] IMedicalHistoryService medicalHistoryService) =>
        {
            try
            {
                var medicalHistory = await medicalHistoryService.GetByPatientDocumentAsync(document);
                if (medicalHistory == null)
                    return Results.NotFound("Medical history not found for this patient document");

                return Results.Ok(medicalHistory);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error retrieving medical history: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPost("", async (
            Shared.DTOs.MedicalHistory.Add.Request request,
            [FromServices] IMedicalHistoryService medicalHistoryService) =>
        {
            try
            {
                // Check if medical history already exists for this patient
                var existingHistory = await medicalHistoryService.ExistsAsync(m => m.PatientId == request.PatientId && !m.IsDeleted);
                if (existingHistory)
                    return Results.Conflict("Medical history already exists for this patient");

                var medicalHistoryId = await medicalHistoryService.AddAsync(request);
                return Results.Created($"/api/medical-histories/{medicalHistoryId}", medicalHistoryId);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error creating medical history: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            Shared.DTOs.MedicalHistory.Edit.Request request,
            [FromServices] IMedicalHistoryService medicalHistoryService) =>
        {
            request = request with { Id = id };

            try
            {
                var medicalHistory = await medicalHistoryService.EditAsync(request);
                return Results.Ok(medicalHistory);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound("Medical history not found");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error updating medical history: {ex.Message}", statusCode: 500);
            }
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IMedicalHistoryService medicalHistoryService) =>
        {
            try
            {
                var deleted = await medicalHistoryService.DeleteAsync(id);
                if (!deleted)
                    return Results.NotFound("Medical history not found");

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error deleting medical history: {ex.Message}", statusCode: 500);
            }
        });

        return group.RequireAuthorization();
    }
}