using MHDTO = Shared.DTOs.MedicalHistory.Get;

namespace Shared.DTOs.Patient.Get;

public sealed record PatientWithMedicalHistoryResponse(
    Guid Id,
    Guid UserId,
    string Name,
    DateOnly BirthDate,
    string Document,
    string Contact,
    string Email,
    string PhoneNumber,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    MHDTO.Response? MedicalHistory
);