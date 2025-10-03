namespace Shared.DTOs.Auth;

public sealed record Register(
    string Username,
    string Email,
    string Password,
    string PhoneNumber
    );
