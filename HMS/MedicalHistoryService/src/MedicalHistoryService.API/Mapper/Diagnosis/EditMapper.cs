using Shared.DTOs.MedicalHistory.Edit;
using Shared.Mapper;

namespace MedicalHistoryService.API.Mapper.Diagnosis;

public class EditMapper : IEditMapper<DiagnosisRequest, Models.Diagnosis>
{
    public Models.Diagnosis ToEntity(DiagnosisRequest request, object original)
    {
        var diagnosis = (Models.Diagnosis)original;
        diagnosis.Description = request.Description;
        diagnosis.Date = request.Date;
        diagnosis.UpdatedAt = DateTime.UtcNow;
        return diagnosis;
    }
}