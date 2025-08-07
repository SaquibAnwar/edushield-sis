using EduShield.Core.Enums;

namespace EduShield.Core.Entities;

public class Faculty(Guid facultyId, string name, string department, string subject, Gender gender)
{
    public Guid FacultyId { get; init; } = facultyId;
    public string Name { get; set; } = name;
    public string Department { get; set; } = department;
    public string Subject { get; set; } = subject;
    public Gender Gender { get; set; } = gender;
    public ICollection<Performance> Performances { get; init; } = [];
}
