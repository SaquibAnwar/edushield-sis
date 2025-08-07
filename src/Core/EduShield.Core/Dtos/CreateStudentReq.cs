using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public sealed record CreateStudentReq(
    string Name,
    string Class,
    string Section,
    Gender Gender);
