namespace BO.DTO.Voucher;

public class CreateVoucherResponseDto
{
    public int VoucherId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal MinAmountRequired { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public bool IsActive { get; set; }
    public string VoucherCode { get; set; } = string.Empty;
    public int RedeemPoint { get; set; }
    public int Quantity { get; set; }
    public int UsedQuantity { get; set; }
    public int RemainingQuantity { get; set; }
}
