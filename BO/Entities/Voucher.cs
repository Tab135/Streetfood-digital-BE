using System.ComponentModel.DataAnnotations;

namespace BO.Entities;

public class Voucher
{
    [Key]
    public int VoucherId { get; set; }

    // public int? QuestId { get; set; } // No quest module yet

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    public decimal DiscountValue { get; set; }
    public decimal MinAmountRequired { get; set; }
    public decimal? MaxDiscountValue { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ExpiredDate { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(100)]
    public string VoucherCode { get; set; } = string.Empty;

    public int RedeemPoint { get; set; }
    public int Quantity { get; set; }
    public int UsedQuantity { get; set; }

    public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
}
