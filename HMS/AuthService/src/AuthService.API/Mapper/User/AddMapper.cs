
using Shared.DTOs.Users.Add;
using Shared.Mapper;

namespace AuthService.API.Mapper.User;

public class AddMapper : IAddMapper<Request, Models.User>
{
    public Models.User ToEntity(Request request)
    => new Models.User
    {
        Id = Guid.NewGuid(),
        Username = request.Username,
        Email = request.Email,
        PhoneNumber = request.PhoneNumber,
        PasswordHash = string.Empty, 
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow),
    };
}
