using EduShield.Core.Dtos;
using FluentValidation;

namespace EduShield.Core.Validators;

public class CreateStudentReqValidator : AbstractValidator<CreateStudentReq>
{
    public CreateStudentReqValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateOfBirth).NotEmpty().LessThan(DateTime.Today);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EnrollmentDate).NotEmpty().GreaterThanOrEqualTo(DateTime.Today);
        RuleFor(x => x.Gender).IsInEnum();
    }
}
