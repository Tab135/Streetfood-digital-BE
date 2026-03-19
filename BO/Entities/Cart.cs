using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class Cart
{
    [Key]
    public int CartId { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    [ForeignKey("Branch")]
    public int? BranchId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
