namespace EduShield.Core.Entities;

public class UserSession : AuditableEntity
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public User? User { get; set; }
    
    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => IsActive && !IsExpired;
    
    // Legacy properties for backward compatibility
    public Guid Id => SessionId;
    public string Token => SessionToken;
}