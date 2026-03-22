using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Cart;

public class CheckoutCartRequest
{
    public int? UserVoucherId { get; set; }

    [MaxLength(255)]
    public string? Table { get; set; }

    [MaxLength(255)]
    public string? PaymentMethod { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be non-negative")]
    public decimal? DiscountAmount { get; set; }

    public bool IsTakeAway { get; set; }
}
