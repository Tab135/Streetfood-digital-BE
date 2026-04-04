namespace BO.DTO.Voucher;

public class UserVoucherResponseDto
{
    public int UserVoucherId { get; set; }
    public int VoucherId { get; set; }
    public string VoucherCode { get; set; } = string.Empty;
    public string VoucherName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string VoucherType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinAmountRequired { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int? CampaignId { get; set; }
    public int Quantity { get; set; }
    public bool IsAvailable { get; set; }
}
