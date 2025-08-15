namespace EduShield.Core.Dtos;

public class FeeDto
{
    public Guid FeeId { get; set; }
    public Guid StudentId { get; set; }
    public string FeeType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysOverdue { get; set; }
    
    // Optional navigation data
    public string? StudentName { get; set; }
}