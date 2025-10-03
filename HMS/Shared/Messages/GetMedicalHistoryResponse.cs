using Shared.DTOs.MedicalHistory.Get;

namespace Shared.Messages;

public record GetMedicalHistoryResponse(
    Guid CorrelationId,
    bool Success,
    Response? MedicalHistory,
    string? ErrorMessage
);