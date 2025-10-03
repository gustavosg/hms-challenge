namespace Shared.DTOs.MedicalHistory.Get;

public sealed record Request(
    Guid? PatientId,
    string? PatientDocument,
    DateTime? FromDate,
    DateTime? ToDate
);