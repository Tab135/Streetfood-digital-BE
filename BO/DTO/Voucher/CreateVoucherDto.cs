using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Voucher;

public class CreateVoucherDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    [RegularExpression("^(AMOUNT|PERCENTAGE)$", ErrorMessage = "Type must be AMOUNT, PERCENTAGE")]
    public string Type { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MinAmountRequired { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxDiscountValue { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string VoucherCode { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int RedeemPoint { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
    public int? CampaignId { get; set; }
    public bool IsActive { get; set; } = true;
}
