using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public class UserProfileDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Role-specific information
    public Guid? StudentId { get; set; }
    public Guid? FacultyId { get; set; }
    
    // Legacy properties for backward compatibility
    public Guid Id => UserId;
    public string Name => FullName;
}