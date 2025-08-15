using System.ComponentModel.DataAnnotations;
using EduShield.Core.Enums;

namespace EduShield.Core.Dtos;

public class UpdateFeeReq
{
    [Required(ErrorMessage = "Fee type is required")]
    public FeeType FeeType { get; set; }
    
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Amount must have at most 2 decimal places")]
    public decimal Amount { get; set; }
    
    [Required(ErrorMessage = "Due date is required")]
    public DateTime DueDate { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
}