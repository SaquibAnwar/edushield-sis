namespace EduShield.Core.Entities;

public class Payment : AuditableEntity
{
    public Guid PaymentId { get; set; }
    public Guid FeeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    
    // Navigation properties
    public Fee? Fee { get; set; }
}