using Shared.Models;

namespace MedicalHistoryService.API.Models;

public class Prescription : BaseEntity
{
    public Guid MedicalHistoryId { get; set; }
    public required string Medication { get; set; }
    public required string Dosage { get; set; }
    public required DateTime Date { get; set; }
    
    // Navigation property
    public MedicalHistory MedicalHistory { get; set; } = null!;
}