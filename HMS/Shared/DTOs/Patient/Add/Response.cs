namespace Shared.DTOs.Patient.Add;

public sealed record Response(
    Guid Id,
    Guid UserId,
    string Name,
    DateOnly BirthDate,
    string Document,
    string Contact,
    string Email,
    string PhoneNumber
);

