using BO.DTO.Order;
using BO.DTO.Payments;

namespace BO.DTO.Cart;

public class CheckoutCartResponseDto
{
    public OrderResponseDto Order { get; set; }
    public PaymentLinkResult Payment { get; set; }
}
