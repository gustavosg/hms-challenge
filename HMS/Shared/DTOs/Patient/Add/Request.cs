namespace Shared.DTOs.Patient.Add;

public sealed record Request(
    Guid UserId,
    string Name,
    DateOnly BirthDate,
    string Document,
    string Contact,
    string Email,
    string PhoneNumber,
    string? Password = null
    );

