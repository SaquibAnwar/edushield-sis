using EduShield.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace EduShield.Core.Dtos;

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public string ExternalId { get; set; } = string.Empty;
    
    [Required]
    public AuthProvider Provider { get; set; }
    
    public UserRole Role { get; set; } = UserRole.Student;
    
    public string? ProfilePictureUrl { get; set; }
    
    // Legacy property for backward compatibility
    public string Name => $"{FirstName} {LastName}".Trim();
}