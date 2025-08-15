namespace EduShield.Core.Dtos;

public class CreatePerformanceReq
{
    public Guid StudentId { get; set; }
    public Guid FacultyId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public decimal Marks { get; set; }
    public decimal MaxMarks { get; set; }
    public DateTime ExamDate { get; set; }
}