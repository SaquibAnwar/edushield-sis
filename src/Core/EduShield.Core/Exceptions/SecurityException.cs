namespace EduShield.Core.Exceptions;

public class SecurityException : Exception
{
    public string? EventType { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public string? UserId { get; }

    public SecurityException(string message) : base(message)
    {
    }

    public SecurityException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public SecurityException(
        string message, 
        string eventType, 
        string? ipAddress = null, 
        string? userAgent = null, 
        string? userId = null) : base(message)
    {
        EventType = eventType;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        UserId = userId;
    }

    public SecurityException(
        string message, 
        string eventType, 
        Exception innerException,
        string? ipAddress = null, 
        string? userAgent = null, 
        string? userId = null) : base(message, innerException)
    {
        EventType = eventType;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        UserId = userId;
    }
}