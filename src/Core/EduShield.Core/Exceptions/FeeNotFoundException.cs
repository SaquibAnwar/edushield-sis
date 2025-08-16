namespace EduShield.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested fee is not found
/// </summary>
public class FeeNotFoundException : Exception
{
    public Guid FeeId { get; }

    public FeeNotFoundException(Guid feeId) 
        : base($"Fee with ID '{feeId}' was not found.")
    {
        FeeId = feeId;
    }

    public FeeNotFoundException(Guid feeId, string message) 
        : base(message)
    {
        FeeId = feeId;
    }

    public FeeNotFoundException(Guid feeId, string message, Exception innerException) 
        : base(message, innerException)
    {
        FeeId = feeId;
    }
}