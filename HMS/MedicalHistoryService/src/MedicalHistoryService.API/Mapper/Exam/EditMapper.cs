using Shared.DTOs.MedicalHistory.Edit;
using Shared.Mapper;

namespace MedicalHistoryService.API.Mapper.Exam;

public class EditMapper : IEditMapper<ExamRequest, Models.Exam>
{
    public Models.Exam ToEntity(ExamRequest request, object original)
    {
        var exam = (Models.Exam)original;
        exam.Type = request.Type;
        exam.Date = request.Date;
        exam.Result = request.Result;
        exam.UpdatedAt = DateTime.UtcNow;
        return exam;
    }
}