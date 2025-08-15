namespace EduShield.Core.Dtos;

public class CreateFeeReq
{
    public Guid StudentId { get; set; }
    public string FeeType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
}