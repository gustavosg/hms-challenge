namespace Shared.DTOs.ExternalExam;

public sealed record Response(
    string Id,
    string PatientDocument,
    string ExamType,
    DateTime ExamDate,
    string Laboratory,
    string Status,
    ExamResult? Result
);

public sealed record ExamResult(
    Dictionary<string, object> Values,
    string? Observations,
    DateTime ResultDate
);