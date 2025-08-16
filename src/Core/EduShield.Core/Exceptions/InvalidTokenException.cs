namespace EduShield.Core.Exceptions;

public class InvalidTokenException : Exception
{
    public InvalidTokenException() : base("Invalid or expired token")
    {
    }

    public InvalidTokenException(string message) : base(message)
    {
    }

    public InvalidTokenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}