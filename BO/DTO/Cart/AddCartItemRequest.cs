using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Cart;

public class AddCartItemRequest
{
    [Required]
    public int BranchId { get; set; }

    [Required]
    public int DishId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
