using FluentValidation;
using FluentValidation.Results;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Validators;

public static class FeeValidationHelper
{
    public static async Task<ValidationResult> ValidateCreateFeeAsync(CreateFeeReq request)
    {
        var validator = new CreateFeeReqValidator();
        return await validator.ValidateAsync(request);
    }

    public static async Task<ValidationResult> ValidateUpdateFeeAsync(UpdateFeeReq request)
    {
        var validator = new UpdateFeeReqValidator();
        return await validator.ValidateAsync(request);
    }

    public static async Task<ValidationResult> ValidateUpdateFeeBusinessRulesAsync(UpdateFeeReq request, Fee existingFee)
    {
        var context = new UpdateFeeValidationContext
        {
            UpdateRequest = request,
            Fee = existingFee
        };
        var validator = new UpdateFeeBusinessValidator();
        return await validator.ValidateAsync(context);
    }

    public static async Task<ValidationResult> ValidatePaymentAsync(PaymentReq request)
    {
        var validator = new PaymentReqValidator();
        return await validator.ValidateAsync(request);
    }

    public static async Task<ValidationResult> ValidatePaymentBusinessRulesAsync(PaymentReq request, Fee fee)
    {
        var context = new PaymentValidationContext
        {
            PaymentRequest = request,
            Fee = fee
        };
        var validator = new PaymentBusinessValidator();
        return await validator.ValidateAsync(context);
    }

    public static bool IsValidDecimalPrecision(decimal value, int maxDecimalPlaces = 2)
    {
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
        return decimalPlaces <= maxDecimalPlaces;
    }

    public static bool IsValidMonetaryAmount(decimal amount)
    {
        return amount > 0 && amount <= 999999.99m && IsValidDecimalPrecision(amount, 2);
    }
}