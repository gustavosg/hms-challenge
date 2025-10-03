namespace Shared.DTOs.Users.Edit;

public sealed record Request(
    Guid Id, 
    string PhoneNumber
);

