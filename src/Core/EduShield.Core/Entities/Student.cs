using EduShield.Core.Enums;

namespace EduShield.Core.Entities;

public class Student
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public Gender Gender { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Performance> Performances { get; set; } = [];
    public ICollection<Fee> Fees { get; set; } = [];
    public Faculty? Faculty { get; set; }
    public Guid? FacultyId { get; set; }
}
