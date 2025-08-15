using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public class FeeDto
{
    public Guid FeeId { get; set; }
    public Guid StudentId { get; set; }
    public FeeType FeeType { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public FeeStatus Status { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Calculated properties
    public decimal OutstandingAmount { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysOverdue { get; set; }
    
    // Optional navigation data
    public string? StudentName { get; set; }
    public List<PaymentDto> Payments { get; set; } = [];
}