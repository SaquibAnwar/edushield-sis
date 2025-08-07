using EduShield.Core.Enums;

namespace EduShield.Core.Entities;

public class Student(Guid studentId, string name, string @class, string section, Gender gender)
{
    public Guid StudentId { get; init; } = studentId;
    public string Name { get; set; } = name;
    public string Class { get; set; } = @class;
    public string Section { get; set; } = section;
    public Gender Gender { get; set; } = gender;
    public ICollection<Performance> Performances { get; init; } = [];
    public ICollection<Fee> Fees { get; init; } = [];
}
