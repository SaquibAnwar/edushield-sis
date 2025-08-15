using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public class FacultyDto
{
    public Guid FacultyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
