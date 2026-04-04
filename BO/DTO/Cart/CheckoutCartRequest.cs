using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Cart;

public class CheckoutCartRequest
{
    public int? VoucherId { get; set; }

    [MaxLength(255)]
    public string? Table { get; set; }

    [MaxLength(255)]
    public string? PaymentMethod { get; set; }

    public bool IsTakeAway { get; set; }
}
