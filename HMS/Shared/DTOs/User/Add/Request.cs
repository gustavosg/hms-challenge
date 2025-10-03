namespace Shared.DTOs.Users.Add;

public sealed record Request(
    string Username,
    string Email,
    string Password,
    string PhoneNumber,
    DateOnly BirthDate
    );