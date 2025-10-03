using Shared.DTOs.Users.Edit;
using Shared.Mapper;

namespace AuthService.API.Mapper.User;

public class EditMapper : IEditMapper<Request, Models.User>
{
    public Models.User ToEntity(Request request, object original)
        => new Models.User
        {
            Id = request.Id,
            Username = ((Models.User)original).Username,
            Email = ((Models.User)original).Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = ((Models.User)original).PasswordHash,
            BirthDate = ((Models.User)original).BirthDate
        };
}
