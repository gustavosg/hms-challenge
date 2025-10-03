using Shared.DTOs.Patient.Add;
using Shared.Mapper;

namespace PatientsService.API.Mapper.Patient;

public class AddMapper : IAddMapper<Request, Models.Patient>
{
    public Models.Patient ToEntity(Request request)
        => new Models.Patient
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name,
            BirthDate = request.BirthDate,
            Document = request.Document,
            Contact = request.Contact,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };
}