namespace EduShield.Core.Exceptions;

/// <summary>
/// Exception thrown when a payment amount is invalid (e.g., exceeds outstanding balance)
/// </summary>
public class InvalidPaymentAmountException : Exception
{
    public decimal PaymentAmount { get; }
    public decimal OutstandingAmount { get; }
    public Guid FeeId { get; }

    public InvalidPaymentAmountException(Guid feeId, decimal paymentAmount, decimal outstandingAmount) 
        : base($"Payment amount {paymentAmount:C} exceeds outstanding balance {outstandingAmount:C} for fee '{feeId}'.")
    {
        FeeId = feeId;
        PaymentAmount = paymentAmount;
        OutstandingAmount = outstandingAmount;
    }

    public InvalidPaymentAmountException(Guid feeId, decimal paymentAmount, decimal outstandingAmount, string message) 
        : base(message)
    {
        FeeId = feeId;
        PaymentAmount = paymentAmount;
        OutstandingAmount = outstandingAmount;
    }

    public InvalidPaymentAmountException(Guid feeId, decimal paymentAmount, decimal outstandingAmount, string message, Exception innerException) 
        : base(message, innerException)
    {
        FeeId = feeId;
        PaymentAmount = paymentAmount;
        OutstandingAmount = outstandingAmount;
    }
}