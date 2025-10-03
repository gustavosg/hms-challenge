using Shared.DTOs.Patient.Get;
using Shared.Mapper;

namespace PatientsService.API.Mapper.Patient;

public class GetMapper : IGetMapper<Models.Patient, Response>
{
    public Response ToDTO(Models.Patient entity)
        => new Response(
            entity.Id,
            entity.UserId,
            entity.Name,
            entity.BirthDate,
            entity.Document,
            entity.Contact,
            entity.Email,
            entity.PhoneNumber,
            entity.CreatedAt,
            entity.UpdatedAt
        );
}