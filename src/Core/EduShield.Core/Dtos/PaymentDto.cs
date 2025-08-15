namespace EduShield.Core.Dtos;

public class PaymentDto
{
    public Guid PaymentId { get; set; }
    public Guid FeeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}