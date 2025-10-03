using Shared.Models;

namespace MedicalHistoryService.API.Models;

public class Exam : BaseEntity
{
    public Guid MedicalHistoryId { get; set; }
    public required string Type { get; set; }
    public required DateTime Date { get; set; }
    public string? Result { get; set; }
    
    // Navigation property
    public MedicalHistory MedicalHistory { get; set; } = null!;
}