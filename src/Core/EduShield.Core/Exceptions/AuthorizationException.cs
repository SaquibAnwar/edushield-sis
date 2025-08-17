namespace EduShield.Core.Exceptions;

public class AuthorizationException : Exception
{
    public string? Resource { get; }
    public string? Action { get; }
    public string? UserId { get; }

    public AuthorizationException(string message) : base(message)
    {
    }

    public AuthorizationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public AuthorizationException(string message, string resource, string action, string? userId = null) 
        : base(message)
    {
        Resource = resource;
        Action = action;
        UserId = userId;
    }

    public AuthorizationException(string message, string resource, string action, string? userId, Exception innerException) 
        : base(message, innerException)
    {
        Resource = resource;
        Action = action;
        UserId = userId;
    }
}