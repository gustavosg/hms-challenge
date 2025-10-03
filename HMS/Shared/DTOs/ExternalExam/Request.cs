namespace Shared.DTOs.ExternalExam;

public sealed record Request(
    string PatientDocument,
    string? ExamType = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
);