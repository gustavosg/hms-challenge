using Shared.DTOs.MedicalHistory.Add;
using Shared.Mapper;

namespace MedicalHistoryService.API.Mapper.MedicalHistory;

public class AddMapper : IAddMapper<Request, Models.MedicalHistory>
{
    public Models.MedicalHistory ToEntity(Request request)
        => new Models.MedicalHistory
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            Document = request.Document,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
            
        };
}