namespace BO.DTO.Cart;

public class CartItemResponseDto
{
    public int DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public string? DishImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class CartResponseDto
{
    public int CartId { get; set; }
    public int UserId { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public decimal TotalAmount { get; set; }
    public List<CartItemResponseDto> Items { get; set; } = new();
}
