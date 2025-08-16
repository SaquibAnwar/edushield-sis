namespace EduShield.Core.Dtos;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public UserDto? User { get; set; }
    public string? SessionToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsNewUser { get; set; }
}