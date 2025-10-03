using Shared.Models;

namespace MedicalHistoryService.API.Models;

public class MedicalHistory : BaseEntity
{
    public required Guid PatientId { get; set; }
    public required string Document { get; set; } // CPF para facilitar consultas
    public string? Notes { get; set; }
    
    public ICollection<Diagnosis> Diagnoses { get; set; } = [];
    public ICollection<Exam> Exams { get; set; } = [];
    public ICollection<Prescription> Prescriptions { get; set; } = [];
}