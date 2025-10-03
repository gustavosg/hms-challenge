using Shared.DTOs.Users.Get;
using Shared.Mapper;

namespace AuthService.API.Mapper.User;

public class GetMapper : IGetMapper<Models.User, Response>
{
    public Response ToDTO(Models.User user)
        => new Response(
            user.Id,
            user.Username,
            user.Email,
            user.PhoneNumber,
            user.BirthDate
        );
}
