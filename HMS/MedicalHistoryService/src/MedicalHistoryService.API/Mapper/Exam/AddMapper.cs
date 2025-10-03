using Shared.DTOs.MedicalHistory.Add;
using Shared.Mapper;

namespace MedicalHistoryService.API.Mapper.Exam;

public class AddMapper : IAddMapper<ExamRequest, Models.Exam>
{
    public Models.Exam ToEntity(ExamRequest request)
        => new Models.Exam
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Date = request.Date,
            Result = request.Result,
            CreatedAt = DateTime.UtcNow
        };
}