using System.ComponentModel.DataAnnotations;

namespace BO.Entities;

public class OrderDish
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int? BranchId { get; set; }

    public int? DishId { get; set; }

    public string DishName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; }
    public virtual BranchDish BranchDish { get; set; }
}
