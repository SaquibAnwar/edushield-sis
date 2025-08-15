namespace EduShield.Core.Dtos;

public class FeesSummaryDto
{
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOverdue { get; set; }
    public int TotalFeeCount { get; set; }
    public int PaidFeeCount { get; set; }
    public int OverdueFeeCount { get; set; }
    public int PendingFeeCount { get; set; }
    public List<FeeDto> Fees { get; set; } = [];
    public List<PaymentDto> RecentPayments { get; set; } = [];
}