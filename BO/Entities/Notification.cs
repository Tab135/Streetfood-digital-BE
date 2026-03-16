using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Message { get; set; }

    public int? ReferenceId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; }
}
