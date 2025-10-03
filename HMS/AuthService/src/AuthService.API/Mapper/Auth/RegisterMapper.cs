using Shared.DTOs.Auth;
using Shared.Mapper;

namespace AuthService.API.Mapper.Auth;

public class RegisterMapper : IAddMapper<Register, Models.User>
{
    public Models.User ToEntity(Register request)
        => new Models.User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PasswordHash = string.Empty, // Será preenchido no service
            CreatedAt = DateTime.UtcNow
        };
}
