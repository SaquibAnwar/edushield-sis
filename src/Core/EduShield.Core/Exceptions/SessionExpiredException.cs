namespace EduShield.Core.Exceptions;

public class SessionExpiredException : Exception
{
    public SessionExpiredException() : base("Session has expired")
    {
    }

    public SessionExpiredException(string message) : base(message)
    {
    }

    public SessionExpiredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}