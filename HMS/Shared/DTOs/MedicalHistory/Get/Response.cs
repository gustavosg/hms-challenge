namespace Shared.DTOs.MedicalHistory.Get;

public sealed record Response(
    Guid Id,
    Guid PatientId,
    string PatientDocument,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<DiagnosisResponse> Diagnoses,
    IEnumerable<ExamResponse> Exams,
    IEnumerable<PrescriptionResponse> Prescriptions
);

public sealed record DiagnosisResponse(
    Guid Id,
    string Description,
    DateTime Date
);

public sealed record ExamResponse(
    Guid Id,
    string Type,
    DateTime Date,
    string? Result
);

public sealed record PrescriptionResponse(
    Guid Id,
    string Medication,
    string Dosage,
    DateTime Date
);