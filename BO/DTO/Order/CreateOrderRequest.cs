using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Order;

public class CreateOrderRequest
{
    [Required]
    public int BranchId { get; set; }

    public int? AppliedVoucherId { get; set; }

    [MaxLength(255)]
    public string? Table { get; set; }

    [MaxLength(255)]
    public string? PaymentMethod { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be non-negative")]
    public decimal? DiscountAmount { get; set; }

    public bool IsTakeAway { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one dish is required")]
    public List<CreateOrderDishRequest> Items { get; set; } = new();
}
