namespace EduShield.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested payment is not found
/// </summary>
public class PaymentNotFoundException : Exception
{
    public Guid PaymentId { get; }

    public PaymentNotFoundException(Guid paymentId) 
        : base($"Payment with ID '{paymentId}' was not found.")
    {
        PaymentId = paymentId;
    }

    public PaymentNotFoundException(Guid paymentId, string message) 
        : base(message)
    {
        PaymentId = paymentId;
    }

    public PaymentNotFoundException(Guid paymentId, string message, Exception innerException) 
        : base(message, innerException)
    {
        PaymentId = paymentId;
    }
}