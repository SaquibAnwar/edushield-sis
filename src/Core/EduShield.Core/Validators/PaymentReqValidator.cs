using FluentValidation;
using EduShield.Core.Dtos;

namespace EduShield.Core.Validators;

public class PaymentReqValidator : AbstractValidator<PaymentReq>
{
    public PaymentReqValidator()
    {
        RuleFor(x => x.Amount)
            .NotEmpty().WithMessage("Payment amount is required")
            .GreaterThan(0).WithMessage("Payment amount must be greater than 0")
            .Must(BeValidDecimalPrecision).WithMessage("Payment amount must have at most 2 decimal places")
            .LessThanOrEqualTo(999999.99m).WithMessage("Payment amount cannot exceed 999,999.99");

        RuleFor(x => x.PaymentDate)
            .NotEmpty().WithMessage("Payment date is required")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Payment date cannot be in the future");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required")
            .MaximumLength(50).WithMessage("Payment method cannot exceed 50 characters")
            .Must(BeValidPaymentMethod).WithMessage("Payment method must be one of: Cash, Check, Credit Card, Debit Card, Bank Transfer, Online Payment");

        RuleFor(x => x.TransactionReference)
            .MaximumLength(100).WithMessage("Transaction reference cannot exceed 100 characters");
    }

    private static bool BeValidDecimalPrecision(decimal amount)
    {
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(amount)[3])[2];
        return decimalPlaces <= 2;
    }

    private static bool BeValidPaymentMethod(string paymentMethod)
    {
        var validMethods = new[] { "Cash", "Check", "Credit Card", "Debit Card", "Bank Transfer", "Online Payment" };
        return validMethods.Contains(paymentMethod, StringComparer.OrdinalIgnoreCase);
    }
}