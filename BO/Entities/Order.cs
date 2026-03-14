using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    [ForeignKey("Branch")]
    public int BranchId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; }
    public virtual Branch Branch { get; set; }
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
