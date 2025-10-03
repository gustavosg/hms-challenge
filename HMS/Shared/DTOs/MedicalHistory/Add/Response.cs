namespace Shared.DTOs.MedicalHistory.Add;

public sealed record Response(
    Guid Id,
    Guid PatientId,
    string PatientDocument,
    string? Notes
);