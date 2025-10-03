namespace Shared.DTOs.MedicalHistory.Edit;

public sealed record Request(
    Guid Id,
    Guid PatientId,
    string Document,
    string? Notes,
    DiagnosisRequest? Diagnosis,
    ExamRequest? Exam,
    PrescriptionRequest? Prescription
);

public sealed record DiagnosisRequest(
    string Description,
    DateTime Date
);

public sealed record ExamRequest(
    string Type,
    DateTime Date,
    string? Result
);

public sealed record PrescriptionRequest(
    string Medication,
    string Dosage,
    DateTime Date
);