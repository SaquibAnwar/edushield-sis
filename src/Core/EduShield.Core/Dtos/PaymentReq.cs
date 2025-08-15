using System.ComponentModel.DataAnnotations;

namespace EduShield.Core.Dtos;

public class PaymentReq
{
    [Required(ErrorMessage = "Payment amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Payment amount must have at most 2 decimal places")]
    public decimal Amount { get; set; }
    
    [Required(ErrorMessage = "Payment date is required")]
    public DateTime PaymentDate { get; set; }
    
    [Required(ErrorMessage = "Payment method is required")]
    [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
    public string PaymentMethod { get; set; } = string.Empty;
    
    [StringLength(100, ErrorMessage = "Transaction reference cannot exceed 100 characters")]
    public string? TransactionReference { get; set; }
}