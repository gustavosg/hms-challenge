namespace Shared.DTOs.Patient.Get;

public sealed record Response(
    Guid Id,
    Guid UserId,
    string Name,
    DateOnly BirthDate,
    string Document,
    string Contact,
    string Email,
    string PhoneNumber,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
