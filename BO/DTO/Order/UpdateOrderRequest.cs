using BO.Entities;

namespace BO.DTO.Order;

public class UpdateOrderRequest
{
    public OrderStatus? Status { get; set; }

    public string? Table { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Note { get; set; }
    public decimal? DiscountAmount { get; set; }
    public bool? IsTakeAway { get; set; }

    public List<CreateOrderDishRequest>? Items { get; set; }
}
