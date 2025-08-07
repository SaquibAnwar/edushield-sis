using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public sealed record StudentDto(
    Guid StudentId,
    string Name,
    string Class,
    string Section,
    Gender Gender);
