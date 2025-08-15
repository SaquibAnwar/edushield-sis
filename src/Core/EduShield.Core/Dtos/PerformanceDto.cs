namespace EduShield.Core.Dtos;

public class PerformanceDto
{
    public Guid PerformanceId { get; set; }
    public Guid StudentId { get; set; }
    public Guid FacultyId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public decimal Marks { get; set; }
    public decimal MaxMarks { get; set; }
    public DateTime ExamDate { get; set; }
    public decimal Percentage { get; set; }
    
    // Optional navigation data
    public string? StudentName { get; set; }
    public string? FacultyName { get; set; }
}