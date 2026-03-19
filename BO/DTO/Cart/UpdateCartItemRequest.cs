using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Cart;

public class UpdateCartItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
