using EduShield.Core.Enums;

namespace EduShield.Core.Entities;

public class Faculty : AuditableEntity
{
    public Faculty() : base()
    {
    }
    
    public Faculty(Guid facultyId, string name, string department, string subject, Gender gender) : base()
    {
        FacultyId = facultyId;
        Name = name;
        Department = department;
        Subject = subject;
        Gender = gender;
    }
    
    public Guid FacultyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public ICollection<Performance> Performances { get; init; } = [];
    
    // Navigation property to Students
    public ICollection<Student> Students { get; init; } = [];
}
