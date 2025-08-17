namespace EduShield.Core.Dtos;

public class UserSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}