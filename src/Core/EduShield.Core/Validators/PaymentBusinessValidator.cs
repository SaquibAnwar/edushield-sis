using FluentValidation;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;

namespace EduShield.Core.Validators;

public class PaymentBusinessValidator : AbstractValidator<PaymentValidationContext>
{
    public PaymentBusinessValidator()
    {
        RuleFor(x => x.PaymentRequest.Amount)
            .LessThanOrEqualTo(x => x.Fee.OutstandingAmount)
            .WithMessage("Payment amount cannot exceed the outstanding fee amount of {PropertyValue}")
            .When(x => x.Fee != null);

        RuleFor(x => x.Fee)
            .NotNull().WithMessage("Fee not found");

        RuleFor(x => x.Fee.Status)
            .NotEqual(FeeStatus.Paid)
            .WithMessage("Cannot make payment on a fee that is already fully paid")
            .When(x => x.Fee != null);
    }
}

public class PaymentValidationContext
{
    public PaymentReq PaymentRequest { get; set; } = null!;
    public Fee Fee { get; set; } = null!;
}