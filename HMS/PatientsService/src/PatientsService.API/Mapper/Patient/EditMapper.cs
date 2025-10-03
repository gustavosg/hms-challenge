using Shared.DTOs.Patient.Edit;
using Shared.Mapper;

namespace PatientsService.API.Mapper.Patient;

public class EditMapper : IEditMapper<Request, Models.Patient>
{
    public Models.Patient ToEntity(Request request, object original)
        => new Models.Patient
        {
            Id = request.Id,
            UserId = ((Models.Patient)original).UserId,
            Name = request.Name,
            BirthDate = request.BirthDate,
            Document = request.Document,
            Contact = request.Contact,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = ((Models.Patient)original).CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = ((Models.Patient)original).IsDeleted
        };
}