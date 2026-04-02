using BO.Entities;

namespace BO.DTO.Order;

public class OrderDishResponseDto
{
    public int DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string? Table { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public bool IsTakeAway { get; set; }
    public DateTime? LockedAt { get; set; }
    public int? OrderXP { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderDishResponseDto> Items { get; set; } = new();
}
