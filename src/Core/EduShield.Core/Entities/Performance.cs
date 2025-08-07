namespace EduShield.Core.Entities;

public class Performance(Guid performanceId, Guid studentId, Guid facultyId, string subject, 
                        decimal marks, decimal maxMarks, DateTime examDate)
{
    public Guid PerformanceId { get; init; } = performanceId;
    public Guid StudentId { get; set; } = studentId;
    public Guid FacultyId { get; set; } = facultyId;
    public string Subject { get; set; } = subject;
    public decimal Marks { get; set; } = marks;
    public decimal MaxMarks { get; set; } = maxMarks;
    public DateTime ExamDate { get; set; } = examDate;
    
    // Navigation properties
    public Student? Student { get; set; }
    public Faculty? Faculty { get; set; }
    
    // Calculated property
    public decimal Percentage => MaxMarks > 0 ? (Marks / MaxMarks) * 100 : 0;
}
