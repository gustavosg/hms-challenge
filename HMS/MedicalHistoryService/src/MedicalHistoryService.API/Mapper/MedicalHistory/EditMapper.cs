using Shared.DTOs.MedicalHistory.Edit;
using Shared.Mapper;

namespace MedicalHistoryService.API.Mapper.MedicalHistory;

public class EditMapper : IEditMapper<Request, Models.MedicalHistory>
{
    public Models.MedicalHistory ToEntity(Request request, object original)
        => new Models.MedicalHistory
        {
            Id = request.Id,
            PatientId = ((Models.MedicalHistory)original).PatientId,
            Document = ((Models.MedicalHistory)original).Document,
            Notes = request.Notes,
            CreatedAt = ((Models.MedicalHistory)original).CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = ((Models.MedicalHistory)original).IsDeleted
        };
}