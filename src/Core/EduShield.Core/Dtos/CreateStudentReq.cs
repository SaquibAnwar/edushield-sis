using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public class CreateStudentReq
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public Gender Gender { get; set; }
    public Guid? FacultyId { get; set; }
}
