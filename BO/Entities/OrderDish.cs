using System.ComponentModel.DataAnnotations;

namespace BO.Entities;

public class OrderDish
{
    public int OrderId { get; set; }

    public int BranchId { get; set; }

    public int DishId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; }
    public virtual BranchDish BranchDish { get; set; }
}
