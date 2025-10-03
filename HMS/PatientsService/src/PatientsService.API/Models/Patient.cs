using System.Text.Json.Serialization;
using Shared.Models;

namespace PatientsService.API.Models;

public class Patient : BaseEntity
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required DateOnly BirthDate { get; set; }
    public required string Document { get; set; }
    public required string Contact { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
}