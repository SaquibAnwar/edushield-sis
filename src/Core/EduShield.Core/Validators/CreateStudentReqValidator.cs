using EduShield.Core.Dtos;
using FluentValidation;

namespace EduShield.Core.Validators;

public class CreateStudentReqValidator : AbstractValidator<CreateStudentReq>
{
    public CreateStudentReqValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Class).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Section).NotEmpty().MaximumLength(5);
    }
}
