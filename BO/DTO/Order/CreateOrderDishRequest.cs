using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Order;

public class CreateOrderDishRequest
{
    [Required]
    public int DishId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
