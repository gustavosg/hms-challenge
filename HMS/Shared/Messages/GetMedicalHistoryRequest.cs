namespace Shared.Messages;

public record GetMedicalHistoryRequest(
    Guid PatientId,
    string PatientDocument,
    Guid CorrelationId
);