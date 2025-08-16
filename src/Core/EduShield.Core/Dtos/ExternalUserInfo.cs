using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public class ExternalUserInfo
{
    public string ExternalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public AuthProvider Provider { get; set; }
    public Dictionary<string, string> Claims { get; set; } = new();
}