namespace EduShield.Core.Exceptions;

/// <summary>
/// Exception thrown when fee business rules are violated
/// </summary>
public class FeeBusinessRuleException : Exception
{
    public string BusinessRule { get; }
    public Guid? FeeId { get; }

    public FeeBusinessRuleException(string businessRule, string message) 
        : base(message)
    {
        BusinessRule = businessRule;
    }

    public FeeBusinessRuleException(string businessRule, string message, Guid feeId) 
        : base(message)
    {
        BusinessRule = businessRule;
        FeeId = feeId;
    }

    public FeeBusinessRuleException(string businessRule, string message, Exception innerException) 
        : base(message, innerException)
    {
        BusinessRule = businessRule;
    }

    public FeeBusinessRuleException(string businessRule, string message, Guid feeId, Exception innerException) 
        : base(message, innerException)
    {
        BusinessRule = businessRule;
        FeeId = feeId;
    }
}