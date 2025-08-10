using EduShield.Core.Enums;

namespace EduShield.Core.Entities;

public class Faculty(Guid facultyId, string name, string department, string subject, Gender gender)
{
    public Faculty() : this(Guid.Empty, string.Empty, string.Empty, string.Empty, Gender.Other)
    {
    }
    
    public Guid FacultyId { get; init; } = facultyId;
    public string Name { get; set; } = name;
    public string Department { get; set; } = department;
    public string Subject { get; set; } = subject;
    public Gender Gender { get; set; } = gender;
    public ICollection<Performance> Performances { get; init; } = [];
    
    // Navigation property to Students
    public ICollection<Student> Students { get; init; } = [];
}
