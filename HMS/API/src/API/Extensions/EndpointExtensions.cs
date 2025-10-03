using FluentValidation;
using FluentValidation.Results;
using API.Integrations.Interfaces;
using Refit;

using DTO = Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using API.Services.Interfaces;

namespace API.Extensions;

public static class EndpointExtensions
{
    private static class Routes
    {
        public const string Patients = "/api/patients";
        public const string Auth = "/api/auth";
        public const string Users = "/api/users";
        public const string MedicalHistories = "/api/medical-histories";
        public const string ExternalExams = "/api/external-exams";
    }

    private static class RoutesAuth
    {
        public const string Login = "/login";
        public const string Register = "/register";
    }

    private static class Tags
    {
        public const string Patients = "Patients";
        public const string Authentication = "Authentication";
        public const string Users = "Users";
        public const string MedicalHistories = "Medical Histories";
        public const string ExternalExams = "External Exams";
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPatientsEndpoints();
        app.MapAuthEndpoints();
        app.MapUserEndpoints();
        app.MapMedicalHistoryEndpoints();
        app.MapExternalExamEndpoints();
        app.MapTestEndpoints();

        return app;
    }

    private static IEndpointRouteBuilder MapPatientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.Patients)
            .RequireAuthorization()
            .WithTags(Tags.Patients);

        group.MapGet("", async (
            [AsParameters] DTO.Patient.Get.Request request,
            [FromServices] IPatientServiceClient patientClient,
            int page = 1,
            int pageSize = 20) =>
        {
            try
            {
                var patients = await patientClient.GetPatientsAsync(request, page, pageSize);
                return Results.Ok(patients);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("No patients found");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Patient service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IPatientServiceClient patientClient) =>
        {
            try
            {
                var patient = await patientClient.GetPatientByIdAsync(id);
                return Results.Ok(patient);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Patient not found");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Patient service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPost("", async (
            DTO.Patient.Add.Request request,
            [FromServices] IValidator<DTO.Patient.Add.Request> validator,
            [FromServices] IPatientServiceClient patientClient,
            [FromServices] IAuthUserServiceClient authClient) =>
        {
            ValidationResult result = await validator.ValidateAsync(request);

            if (!result.IsValid)
                return Results.BadRequest(result.Errors);

            try
            {
                var userRequest = new DTO.Users.Add.Request(
                    request.Name,
                    request.Email,
                    request.Password ?? "DefaultPassword123!",
                    request.PhoneNumber,
                    request.BirthDate
                );

                var userId = await authClient.AddUserAsync(userRequest);

                var patientRequest = request with { UserId = userId };
                var patientId = await patientClient.AddPatientAsync(patientRequest);
                return Results.Created($"/api/patients/{patientId}", new { Id = patientId });
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Results.Conflict("Patient with this document or email already exists");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Patient service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            DTO.Patient.Edit.Request request,
            [FromServices] IValidator<DTO.Patient.Edit.Request> validator,
            [FromServices] IPatientServiceClient patientClient) =>
        {
            request = request with { Id = id };

            ValidationResult result = await validator.ValidateAsync(request);

            if (!result.IsValid)
                return Results.BadRequest(result.Errors);

            try
            {
                var patient = await patientClient.EditPatientAsync(id, request);
                return Results.Ok(patient);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Patient not found");
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Results.Conflict("Another patient with this document or email already exists");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Patient service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IPatientServiceClient patientClient) =>
        {
            try
            {
                await patientClient.DeletePatientAsync(id);
                return Results.NoContent();
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Patient not found");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Patient service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/{id:guid}/with-medical-history", async (
            Guid id,
            [FromServices] IPatientServiceClient patientClient,
            [FromServices] IMedicalHistoryServiceClient medicalHistoryClient) =>
        {
            try
            {
                var patient = await patientClient.GetPatientByIdAsync(id);
                
                try
                {
                    var medicalHistory = await medicalHistoryClient.GetMedicalHistoryByPatientIdAsync(id);
                    
                    var response = new DTO.Patient.Get.PatientWithMedicalHistoryResponse(
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
                        MedicalHistory: medicalHistory
                    );
                    
                    return Results.Ok(response);
                }
                catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var response = new DTO.Patient.Get.PatientWithMedicalHistoryResponse(
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
                        MedicalHistory: null
                    );
                    
                    return Results.Ok(response);
                }
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Patient not found");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/{id:guid}/with-medical-history-rabbitmq", async (
            Guid id,
            [FromServices] IPatientServiceClient patientClient,
            [FromServices] Shared.Services.Messaging.IMessageBrokerService messageBroker) =>
        {
            try
            {
                var patient = await patientClient.GetPatientByIdAsync(id);
                
                try
                {
                    var request = new Shared.Messages.GetMedicalHistoryRequest(
                        PatientId: patient.Id,
                        PatientDocument: patient.Document,
                        CorrelationId: Guid.NewGuid()
                    );

                    var timeout = TimeSpan.FromSeconds(10);
                    var medicalHistoryResponse = await messageBroker.RequestMedicalHistoryAsync(request, timeout);
                    
                    if (medicalHistoryResponse?.Success == true)
                    {
                        var response = new DTO.Patient.Get.PatientWithMedicalHistoryResponse(
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
                            MedicalHistory: medicalHistoryResponse.MedicalHistory
                        );
                        
                        return Results.Ok(response);
                    }
                    else
                    {
                        var response = new DTO.Patient.Get.PatientWithMedicalHistoryResponse(
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
                            MedicalHistory: null
                        );
                        
                        return Results.Ok(response);
                    }
                }
                catch (Exception rabbitEx)
                {
                    var response = new DTO.Patient.Get.PatientWithMedicalHistoryResponse(
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
                        MedicalHistory: null
                    );
                    
                    return Results.Ok(response);
                }
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Patient not found");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Service error: {ex.Message}", statusCode: 500);
            }
        });

        return group;
    }

    private static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.Auth).WithTags(Tags.Authentication);

        group.MapPost(RoutesAuth.Login, async (
            DTO.Auth.Login request,
            [FromServices] IAuthUserServiceClient authClient) =>
        {
            try
            {
                var response = await authClient.LoginAsync(request);
                return Results.Ok(response);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("User does not exist");
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return Results.Unauthorized();
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Authentication service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPost(RoutesAuth.Register, async (
            DTO.Auth.Register request,
            [FromServices] IValidator<DTO.Auth.Register> validator,
            [FromServices] IAuthUserServiceClient authClient) =>
        {
            ValidationResult result = await validator.ValidateAsync(request);

            if (!result.IsValid)
                return Results.BadRequest(result.Errors);

            try
            {
                var response = await authClient.RegisterAsync(request);
                return Results.Created($"/api/users/{response}", response);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Results.Conflict("User already exists");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Authentication service error: {ex.Message}", statusCode: 500);
            }
        });

        return group;
    }

    private static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.Users).WithTags(Tags.Users);

        group.MapGet("", async (
            [AsParameters] DTO.Users.Get.Request request,
            [FromServices] IAuthUserServiceClient authClient,
            int page = 1,
            int pageSize = 20
            ) =>
        {
            try
            {
                var users = await authClient.GetUsersAsync(request, page, pageSize);
                return Results.Ok(users);
            }
            catch (ApiException ex)
            {
                return Results.Problem($"User service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IAuthUserServiceClient authClient) =>
        {
            try
            {
                var user = await authClient.GetUserByIdAsync(id);
                return Results.Ok(user);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("User does not exist");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"User service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPost("", async (
            DTO.Users.Add.Request request,
            [FromServices] IValidator<DTO.Users.Add.Request> validator,
            [FromServices] IAuthUserServiceClient authClient) =>
        {
            ValidationResult result = await validator.ValidateAsync(request);

            if (!result.IsValid)
                return Results.BadRequest(result.Errors);

            try
            {
                var response = await authClient.AddUserAsync(request);
                return Results.Created($"/api/users/{response}", response);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Results.Conflict("User already exists");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"User service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            DTO.Users.Edit.Request request,
            [FromServices] IAuthUserServiceClient authClient) =>
        {
            request = request with { Id = id };
                                    
            try
            {
                var response = await authClient.EditUserAsync(id, request);
                return Results.Ok(response);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("User does not exist");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"User service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IAuthUserServiceClient authClient) =>
        {
            try
            {
                await authClient.DeleteUserAsync(id);
                return Results.NoContent();
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("User does not exist");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"User service error: {ex.Message}", statusCode: 500);
            }
        });

        return group.RequireAuthorization();
    }

    private static IEndpointRouteBuilder MapMedicalHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.MedicalHistories)
            .RequireAuthorization()
            .WithTags(Tags.MedicalHistories);

        group.MapGet("", async (
            [AsParameters] DTO.MedicalHistory.Get.Request request,
            [FromServices] IMedicalHistoryServiceClient medicalHistoryClient,
            int page = 1,
            int pageSize = 20) =>
        {
            try
            {
                var medicalHistories = await medicalHistoryClient.GetMedicalHistoriesAsync(request, page, pageSize);
                return Results.Ok(medicalHistories);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("No medical histories found");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Medical history service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IMedicalHistoryServiceClient medicalHistoryClient) =>
        {
            try
            {
                var medicalHistory = await medicalHistoryClient.GetMedicalHistoryByIdAsync(id);
                return Results.Ok(medicalHistory);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Medical history not found");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Medical history service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/patient/{patientId:guid}", async (
            Guid patientId,
            [FromServices] IMedicalHistoryServiceClient medicalHistoryClient) =>
        {
            try
            {
                var medicalHistory = await medicalHistoryClient.GetMedicalHistoryByPatientIdAsync(patientId);
                return Results.Ok(medicalHistory);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Medical history not found for this patient");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Medical history service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/patient/document/{document}", async (
            string document,
            [FromServices] IMedicalHistoryServiceClient medicalHistoryClient) =>
        {
            try
            {
                var medicalHistory = await medicalHistoryClient.GetMedicalHistoryByPatientDocumentAsync(document);
                return Results.Ok(medicalHistory);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Medical history not found for this patient document");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Medical history service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPost("", async (
            DTO.MedicalHistory.Add.Request request,
            [FromServices] IValidator<DTO.MedicalHistory.Add.Request> validator,
            [FromServices] IMedicalHistoryServiceClient medicalHistoryClient) =>
        {
            ValidationResult result = await validator.ValidateAsync(request);

            if (!result.IsValid)
                return Results.BadRequest(result.Errors);

            try
            {
                var medicalHistoryId = await medicalHistoryClient.AddMedicalHistoryAsync(request);
                return Results.Created($"/api/medical-histories/{medicalHistoryId}", new { Id = medicalHistoryId });
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Results.Conflict("Medical history already exists for this patient");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Medical history service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            DTO.MedicalHistory.Edit.Request request,
            [FromServices] IValidator<DTO.MedicalHistory.Edit.Request> validator,
            [FromServices] IMedicalHistoryServiceClient medicalHistoryClient) =>
        {
            request = request with { Id = id };

            ValidationResult result = await validator.ValidateAsync(request);

            if (!result.IsValid)
                return Results.BadRequest(result.Errors);

            try
            {
                var medicalHistory = await medicalHistoryClient.EditMedicalHistoryAsync(id, request);
                return Results.Ok(medicalHistory);
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Medical history not found");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Medical history service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IMedicalHistoryServiceClient medicalHistoryClient) =>
        {
            try
            {
                await medicalHistoryClient.DeleteMedicalHistoryAsync(id);
                return Results.NoContent();
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Results.NotFound("Medical history not found");
            }
            catch (ApiException ex)
            {
                return Results.Problem($"Medical history service error: {ex.Message}", statusCode: 500);
            }
        });

        return group;
    }

    private static IEndpointRouteBuilder MapExternalExamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.ExternalExams)
            .RequireAuthorization()
            .WithTags(Tags.ExternalExams);

        group.MapGet("", async (
            [AsParameters] DTO.ExternalExam.Request request,
            [FromServices] IExternalExamService externalExamService) =>
        {
            try
            {
                var exams = await externalExamService.GetExternalExamsAsync(request);
                return Results.Ok(exams);
            }
            catch (Exception ex)
            {
                return Results.Problem($"External exam service error: {ex.Message}", statusCode: 500);
            }
        });

        group.MapGet("/{examId}", async (
            string examId,
            [FromServices] IExternalExamService externalExamService) =>
        {
            try
            {
                var exam = await externalExamService.GetExternalExamByIdAsync(examId);
                
                if (exam == null)
                    return Results.NotFound("External exam not found");
                    
                return Results.Ok(exam);
            }
            catch (Exception ex)
            {
                return Results.Problem($"External exam service error: {ex.Message}", statusCode: 500);
            }
        });

        return group;
    }

    private static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/test")
            .WithTags("Test");

        group.MapGet("/rabbitmq-test/{patientId:guid}", async (
            Guid patientId,
            [FromServices] Shared.Services.Messaging.IMessageBrokerService messageBroker) =>
        {
            try
            {
                var serviceType = messageBroker.GetType().Name;
                
                if (serviceType == "NullMessageBrokerService")
                {
                    return Results.Ok(new
                    {
                        Success = false,
                        PatientId = patientId,
                        Message = "RabbitMQ service is not available (using null implementation)",
                        ServiceType = serviceType,
                        Timestamp = DateTime.UtcNow
                    });
                }

                var request = new Shared.Messages.GetMedicalHistoryRequest(
                    PatientId: patientId,
                    PatientDocument: "12345678901",
                    CorrelationId: Guid.NewGuid()
                );

                var timeout = TimeSpan.FromSeconds(10);
                var response = await messageBroker.RequestMedicalHistoryAsync(request, timeout);

                return Results.Ok(new
                {
                    Success = response?.Success ?? false,
                    PatientId = patientId,
                    CorrelationId = request.CorrelationId,
                    Response = response,
                    ServiceType = serviceType,
                    Timestamp = DateTime.UtcNow,
                    Message = response?.Success == true ? "RabbitMQ communication successful" : $"No response or failed: {response?.ErrorMessage}"
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new
                {
                    Success = false,
                    PatientId = patientId,
                    Error = ex.Message,
                    StackTrace = ex.StackTrace,
                    Timestamp = DateTime.UtcNow,
                    Message = "RabbitMQ communication failed with exception"
                });
            }
        });

        group.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                Status = "Healthy",
                Service = "API Gateway",
                Timestamp = DateTime.UtcNow
            });
        });

        return group;
    }
}
