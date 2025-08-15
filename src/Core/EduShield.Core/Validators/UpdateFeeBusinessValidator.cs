using FluentValidation;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;

namespace EduShield.Core.Validators;

public class UpdateFeeBusinessValidator : AbstractValidator<UpdateFeeValidationContext>
{
    public UpdateFeeBusinessValidator()
    {
        RuleFor(x => x.Fee)
            .NotNull().WithMessage("Fee not found");

        RuleFor(x => x.Fee.Status)
            .NotEqual(FeeStatus.Paid)
            .WithMessage("Cannot modify a fee that is already fully paid")
            .When(x => x.Fee != null);

        RuleFor(x => x.UpdateRequest.Amount)
            .GreaterThanOrEqualTo(x => x.Fee.PaidAmount)
            .WithMessage("New fee amount cannot be less than the amount already paid ({PropertyValue})")
            .When(x => x.Fee != null && x.Fee.PaidAmount > 0);

        // Validate due date changes for fees with payments
        RuleFor(x => x.UpdateRequest.DueDate)
            .Must((context, dueDate) => BeValidDueDateForPaidFee(context.Fee, dueDate))
            .WithMessage("Cannot set due date in the past for fees with existing payments")
            .When(x => x.Fee != null && x.Fee.PaidAmount > 0);
    }

    private static bool BeValidDueDateForPaidFee(Fee fee, DateTime newDueDate)
    {
        if (fee.PaidAmount > 0 && fee.Payments != null && fee.Payments.Any())
        {
            // If there are payments, don't allow setting due date before the earliest payment date
            var earliestPaymentDate = fee.Payments.Min(p => p.PaymentDate.Date);
            if (newDueDate.Date < earliestPaymentDate)
            {
                return false;
            }
        }
        return true;
    }
}

public class UpdateFeeValidationContext
{
    public UpdateFeeReq UpdateRequest { get; set; } = null!;
    public Fee Fee { get; set; } = null!;
}