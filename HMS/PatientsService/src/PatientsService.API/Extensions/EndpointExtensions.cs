using Microsoft.AspNetCore.Mvc;
using PatientsService.API.Services.Interfaces;
using Shared.DTOs;
using Shared.Services.Messaging;
using Shared.Messages;
using Shared.DTOs.Patient.Get;

namespace PatientsService.API.Extensions;

public static class EndpointExtensions
{
    private static class Routes
    {
        public const string Patients = "/api/patients";
    }

    private static class Tags
    {
        public const string Patients = "Patients";
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPatientsEndpoints();
        return app;
    }

    private static IEndpointRouteBuilder MapPatientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.Patients)
            .RequireAuthorization()
            .WithTags(Tags.Patients);

        group.MapGet("", async (
            [AsParameters] Shared.DTOs.Patient.Get.Request request,
            [FromServices] IPatientService patientService,
            int page = 1,
            int pageSize = 20) =>
        {
            try
            {
                var patients = await patientService.GetAsync(request, page, pageSize);
                return Results.Ok(patients);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error retrieving patients: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IPatientService patientService) =>
        {
            try
            {
                var patient = await patientService.GetAsync(id);
                return Results.Ok(patient);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound("Patient not found");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error retrieving patient: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/{id:guid}/with-medical-history", async (
            Guid id,
            [FromServices] IPatientService patientService,
            [FromServices] IMessageBrokerService messageBroker) =>
        {
            try
            {
                // 1. Buscar paciente normalmente
                var patient = await patientService.GetAsync(id);

                // 2. Fazer request via RabbitMQ para buscar hist�rico m�dico
                var request = new GetMedicalHistoryRequest(
                    PatientId: patient.Id,
                    PatientDocument: patient.Document,
                    CorrelationId: Guid.NewGuid()
                );

                GetMedicalHistoryResponse? response = await messageBroker.RequestMedicalHistoryAsync(request, TimeSpan.FromSeconds(10));

                Shared.DTOs.Patient.Get.Response? mappedMedicalHistory = null;
                if (response?.Success == true && response.MedicalHistory != null)
                {
                    var mh = response.MedicalHistory;
                    mappedMedicalHistory = new Response(
                        mh.PatientId,
                        patient.UserId, 
                        patient.Name,
                        patient.BirthDate,
                        mh.PatientDocument,
                        patient.Contact,
                        patient.Email,
                        patient.PhoneNumber,
                        mh.CreatedAt,
                        mh.UpdatedAt
                    );
                }

                var combinedResponse = new PatientWithMedicalHistoryResponse(
                    Id: patient.Id,
                    UserId: patient.UserId,
                    Name: patient.Name,
                    BirthDate: patient.BirthDate,
                    Document: patient.Document,
                    Contact: patient.Contact,
                    Email: patient.Email,
                    PhoneNumber: patient.PhoneNumber,
                    CreatedAt: patient.CreatedAt,
                    UpdatedAt: patient.UpdatedAt,
                    MedicalHistory: response?.MedicalHistory
                );

                return Results.Ok(combinedResponse);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound("Patient not found");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error retrieving patient with medical history: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPost("", async (
            Shared.DTOs.Patient.Add.Request request,
            [FromServices] IPatientService patientService) =>
        {
            try
            {
                // Check if patient with same document already exists
                var documentExists = await patientService.ExistsAsync(p => p.Document == request.Document && !p.IsDeleted);
                if (documentExists)
                    return Results.Conflict("Patient with this document already exists");

                var emailExists = await patientService.ExistsAsync(p => p.Email == request.Email && !p.IsDeleted);
                if (emailExists)
                    return Results.Conflict("Patient with this email already exists");

                var patientId = await patientService.AddAsync(request);
                return Results.Created($"/api/patients/{patientId}", patientId);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error creating patient: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            Shared.DTOs.Patient.Edit.Request request,
            [FromServices] IPatientService patientService) =>
        {
            request = request with { Id = id };

            try
            {
                // Check if another patient with same document already exists
                var documentExists = await patientService.ExistsAsync(p => p.Document == request.Document && p.Id != id && !p.IsDeleted);
                if (documentExists)
                    return Results.Conflict("Another patient with this document already exists");

                var emailExists = await patientService.ExistsAsync(p => p.Email == request.Email && p.Id != id && !p.IsDeleted);
                if (emailExists)
                    return Results.Conflict("Another patient with this email already exists");

                var patient = await patientService.EditAsync(request);
                return Results.Ok(patient);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound("Patient not found");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error updating patient: {ex.Message}", statusCode: 500);
            }
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IPatientService patientService) =>
        {
            try
            {
                var deleted = await patientService.DeleteAsync(id);
                if (!deleted)
                    return Results.NotFound("Patient not found");

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error deleting patient: {ex.Message}", statusCode: 500);
            }
        });

        return group;
    }
}