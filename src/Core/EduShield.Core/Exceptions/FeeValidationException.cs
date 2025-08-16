namespace EduShield.Core.Exceptions;

/// <summary>
/// Exception thrown when fee validation fails
/// </summary>
public class FeeValidationException : Exception
{
    public Dictionary<string, string[]> ValidationErrors { get; }

    public FeeValidationException(string message) 
        : base(message)
    {
        ValidationErrors = new Dictionary<string, string[]>();
    }

    public FeeValidationException(string message, Dictionary<string, string[]> validationErrors) 
        : base(message)
    {
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
    }

    public FeeValidationException(string message, Exception innerException) 
        : base(message, innerException)
    {
        ValidationErrors = new Dictionary<string, string[]>();
    }

    public FeeValidationException(string message, Dictionary<string, string[]> validationErrors, Exception innerException) 
        : base(message, innerException)
    {
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
    }
}