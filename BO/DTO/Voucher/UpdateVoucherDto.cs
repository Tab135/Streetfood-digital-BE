using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Voucher;

public class UpdateVoucherDto
{
    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    [RegularExpression("^(AMOUNT|FIXED|PERCENT|PERCENTAGE)$", ErrorMessage = "Type must be AMOUNT, FIXED, PERCENT, or PERCENTAGE")]
    public string? Type { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DiscountValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MinAmountRequired { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxDiscountValue { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [MaxLength(100)]
    public string? VoucherCode { get; set; }

    [Range(0, int.MaxValue)]
    public int? RedeemPoint { get; set; }

    [Range(0, int.MaxValue)]
    public int? Quantity { get; set; }

    [Range(0, int.MaxValue)]
    public int? UsedQuantity { get; set; }

    public bool? IsActive { get; set; }
}
