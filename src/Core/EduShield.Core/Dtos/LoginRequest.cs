using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public class LoginRequest
{
    public AuthProvider Provider { get; set; }
    public string IdToken { get; set; } = string.Empty;
}