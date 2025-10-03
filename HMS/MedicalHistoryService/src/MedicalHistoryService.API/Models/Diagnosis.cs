using Shared.Models;

namespace MedicalHistoryService.API.Models;

public class Diagnosis : BaseEntity
{
    public Guid MedicalHistoryId { get; set; }
    public required string Description { get; set; }
    public required DateTime Date { get; set; }
    
    public MedicalHistory MedicalHistory { get; set; } = null!;
}