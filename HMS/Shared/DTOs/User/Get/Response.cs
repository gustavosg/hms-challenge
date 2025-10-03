namespace Shared.DTOs.Users.Get;

public sealed record Response(
    Guid Id,
    string Username,
    string Email,
    string PhoneNumber ,
    DateOnly BirthDate
    );
