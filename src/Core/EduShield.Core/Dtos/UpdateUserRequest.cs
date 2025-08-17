using EduShield.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace EduShield.Core.Dtos;

public class UpdateUserRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? FirstName { get; set; }
    
    [StringLength(100, MinimumLength = 1)]
    public string? LastName { get; set; }
    
    [Phone]
    public string? PhoneNumber { get; set; }
    
    public UserRole? Role { get; set; }
    
    public bool? IsActive { get; set; }
    
    public string? ProfilePictureUrl { get; set; }
    
    // Legacy property for backward compatibility
    public string? Name => !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName) 
        ? $"{FirstName} {LastName}".Trim() 
        : null;
}