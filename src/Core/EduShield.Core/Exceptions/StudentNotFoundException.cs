namespace EduShield.Core.Exceptions;

/// <summary>
/// Exception thrown when a referenced student is not found
/// </summary>
public class StudentNotFoundException : Exception
{
    public Guid StudentId { get; }

    public StudentNotFoundException(Guid studentId) 
        : base($"Student with ID '{studentId}' was not found.")
    {
        StudentId = studentId;
    }

    public StudentNotFoundException(Guid studentId, string message) 
        : base(message)
    {
        StudentId = studentId;
    }

    public StudentNotFoundException(Guid studentId, string message, Exception innerException) 
        : base(message, innerException)
    {
        StudentId = studentId;
    }
}