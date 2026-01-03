using System;
using System.ComponentModel.DataAnnotations;

namespace BO.Entities;

public class UserBadge
{
    public int UserId { get; set; }

    public int BadgeId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}