using EduShield.Core.Enums;

namespace EduShield.Core.Entities;

public class User : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty; // From OIDC provider
    public AuthProvider Provider { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
    
    // Navigation properties
    public ICollection<UserSession> Sessions { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
    
    // Computed properties
    public string FullName => $"{FirstName} {LastName}".Trim();
}