using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public class CreateFacultyReq
{
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Gender Gender { get; set; }
}

