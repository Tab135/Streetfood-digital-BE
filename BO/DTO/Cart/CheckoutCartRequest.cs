using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Cart;

public class CheckoutCartRequest
{
    [Required]
    public int BranchId { get; set; }

    public int? VoucherId { get; set; }

    [MaxLength(255)]
    public string? Table { get; set; }

    [MaxLength(255)]
    public string? PaymentMethod { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public bool IsTakeAway { get; set; }
}
