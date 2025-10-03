using Shared.DTOs.MedicalHistory.Add;
using Shared.Mapper;

namespace MedicalHistoryService.API.Mapper.Prescription;

public class AddMapper : IAddMapper<PrescriptionRequest, Models.Prescription>
{
    public Models.Prescription ToEntity(PrescriptionRequest request)
        => new Models.Prescription
        {
            Id = Guid.NewGuid(),
            Medication = request.Medication,
            Dosage = request.Dosage,
            Date = request.Date,
            CreatedAt = DateTime.UtcNow
        };
}