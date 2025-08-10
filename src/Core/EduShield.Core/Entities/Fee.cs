namespace EduShield.Core.Entities;

public class Fee(Guid feeId, Guid studentId, string feeType, decimal amount, 
                DateTime dueDate, bool isPaid = false, DateTime? paidDate = null)
{
    public Fee() : this(Guid.Empty, Guid.Empty, string.Empty, 0, DateTime.UtcNow)
    {
    }
    
    public Guid FeeId { get; init; } = feeId;
    public Guid StudentId { get; set; } = studentId;
    public string FeeType { get; set; } = feeType;
    public decimal Amount { get; set; } = amount;
    public DateTime DueDate { get; set; } = dueDate;
    public bool IsPaid { get; set; } = isPaid;
    public DateTime? PaidDate { get; set; } = paidDate;
    
    // Navigation property
    public Student? Student { get; set; }
    
    // Calculated properties
    public bool IsOverdue => !IsPaid && DateTime.UtcNow > DueDate;
    public int DaysOverdue => IsOverdue ? (DateTime.UtcNow - DueDate).Days : 0;
}
