using FluentValidation;
using EduShield.Core.Dtos;
using EduShield.Core.Enums;

namespace EduShield.Core.Validators;

public class CreateFeeReqValidator : AbstractValidator<CreateFeeReq>
{
    public CreateFeeReqValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required")
            .NotEqual(Guid.Empty).WithMessage("Student ID must be a valid GUID");

        RuleFor(x => x.FeeType)
            .IsInEnum().WithMessage("Fee type must be a valid value");

        RuleFor(x => x.Amount)
            .NotEmpty().WithMessage("Amount is required")
            .GreaterThan(0).WithMessage("Amount must be greater than 0")
            .Must(BeValidDecimalPrecision).WithMessage("Amount must have at most 2 decimal places")
            .LessThanOrEqualTo(999999.99m).WithMessage("Amount cannot exceed 999,999.99");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required")
            .Must(BeValidDueDate).WithMessage("Due date cannot be in the past");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }

    private static bool BeValidDecimalPrecision(decimal amount)
    {
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(amount)[3])[2];
        return decimalPlaces <= 2;
    }

    private static bool BeValidDueDate(DateTime dueDate)
    {
        return dueDate.Date >= DateTime.Today;
    }
}