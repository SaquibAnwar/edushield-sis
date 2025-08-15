using FluentValidation;
using EduShield.Core.Dtos;

namespace EduShield.Core.Validators;

public class CreateFacultyReqValidator : AbstractValidator<CreateFacultyReq>
{
    public CreateFacultyReqValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("Department is required")
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required")
            .MaximumLength(100).WithMessage("Subject cannot exceed 100 characters");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Gender must be a valid value");
    }
}
