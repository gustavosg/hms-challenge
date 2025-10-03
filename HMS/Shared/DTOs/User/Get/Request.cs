namespace Shared.DTOs.Users.Get;

public sealed record Request(
    string? Username,
    string? Email,
    string? PhoneNumber,
    DateOnly? BirthDateMin,
    DateOnly? BirthDateMax
    );
