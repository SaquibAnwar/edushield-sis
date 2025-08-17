using EduShield.Core.Enums;

namespace EduShield.Core.Entities;

public class Fee : AuditableEntity
{
    public Guid FeeId { get; set; }
    public Guid StudentId { get; set; }
    
    // Backward compatibility property for tests
    public Guid Id => FeeId;
    public FeeType FeeType { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public FeeStatus Status { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    
    // Navigation properties
    public Student? Student { get; set; }
    public ICollection<Payment> Payments { get; set; } = [];
    
    // Calculated properties
    public decimal OutstandingAmount => Amount - PaidAmount;
    public bool IsOverdue => DateTime.UtcNow > DueDate && OutstandingAmount > 0;
    public int DaysOverdue => IsOverdue ? (DateTime.UtcNow - DueDate).Days : 0;
}