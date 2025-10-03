using Shared.DTOs.MedicalHistory.Edit;
using Shared.Mapper;

namespace MedicalHistoryService.API.Mapper.Prescription;

public class EditMapper : IEditMapper<PrescriptionRequest, Models.Prescription>
{
    public Models.Prescription ToEntity(PrescriptionRequest request, object original)
    {
        var prescription = (Models.Prescription)original;
        prescription.Medication = request.Medication;
        prescription.Dosage = request.Dosage;
        prescription.Date = request.Date;
        prescription.UpdatedAt = DateTime.UtcNow;
        return prescription;
    }
}