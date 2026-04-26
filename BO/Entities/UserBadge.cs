using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class UserBadge
{
    public int UserId { get; set; }

    public int BadgeId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsSelected { get; set; } = false;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("BadgeId")]
    public virtual Badge Badge { get; set; } = null!;
}