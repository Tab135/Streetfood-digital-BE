namespace BO.DTO.Voucher;

public class UserVoucherResponseDto
{
    public int UserVoucherId { get; set; }
    public int VoucherId { get; set; }
    public string VoucherCode { get; set; } = string.Empty;
    public string VoucherName { get; set; } = string.Empty;
    public string VoucherType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public int Quantity { get; set; }
}
