using Shared.DTOs.MedicalHistory.Get;
using Shared.Mapper;

namespace MedicalHistoryService.API.Mapper.MedicalHistory;

public class GetMapper : IGetMapper<Models.MedicalHistory, Response>
{
    public Response ToDTO(Models.MedicalHistory entity)
        => new Response(
            entity.Id,
            entity.PatientId,
            entity.Document,
            entity.Notes,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.Diagnoses.Select(d => new DiagnosisResponse(
                d.Id,
                d.Description,
                d.Date
            )),
            entity.Exams.Select(e => new ExamResponse(
                e.Id,
                e.Type,
                e.Date,
                e.Result
            )),
            entity.Prescriptions.Select(p => new PrescriptionResponse(
                p.Id,
                p.Medication,
                p.Dosage,
                p.Date
            ))
        );
}