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

    [ForeignKey("UserVoucher")]
    public int? UserVoucherId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [StringLength(255)]
    public string? Table { get; set; }

    [StringLength(255)]
    public string? PaymentMethod { get; set; }

    [StringLength(20)]
    public string? CompletionCode { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }

    public int? OrderXP { get; set; }

    public bool IsTakeAway { get; set; }
    public DateTime? LockedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; }
    public virtual Branch Branch { get; set; }
    public virtual UserVoucher? UserVoucher { get; set; }
    public virtual ICollection<OrderDish> OrderDishes { get; set; } = new List<OrderDish>();
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
