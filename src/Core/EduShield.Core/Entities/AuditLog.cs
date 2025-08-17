namespace EduShield.Core.Entities;

public class AuditLog : AuditableEntity
{
    public Guid AuditId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AdditionalData { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
    
    // Legacy properties for backward compatibility
    public Guid Id => AuditId;
    public DateTime Timestamp => CreatedAt;
    public string? Details => AdditionalData;
}