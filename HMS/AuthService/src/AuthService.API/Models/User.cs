using System.Text.Json.Serialization;

using Shared.Models;

namespace AuthService.API.Models;

public class User : BaseEntity
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    [JsonIgnore]
    public string PasswordHash { get; set; }
    public required string PhoneNumber { get; set; }
    public required DateOnly BirthDate { get; set; }
}
