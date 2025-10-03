using Shared.DTOs.MedicalHistory.Add;
using Shared.Mapper;

namespace MedicalHistoryService.API.Mapper.Diagnosis;

public class AddMapper : IAddMapper<DiagnosisRequest, Models.Diagnosis>
{
    public Models.Diagnosis ToEntity(DiagnosisRequest request)
        => new Models.Diagnosis
        {
            Id = Guid.NewGuid(),
            Description = request.Description,
            Date = request.Date,
            CreatedAt = DateTime.UtcNow
        };
}