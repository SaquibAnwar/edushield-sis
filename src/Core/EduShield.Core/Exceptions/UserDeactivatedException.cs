namespace EduShield.Core.Exceptions;

public class UserDeactivatedException : Exception
{
    public UserDeactivatedException() : base("User account has been deactivated")
    {
    }

    public UserDeactivatedException(string message) : base(message)
    {
    }

    public UserDeactivatedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}