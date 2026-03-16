using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class VendorReply
{
    [Key]
    public int VendorReplyId { get; set; }

    [ForeignKey("Feedback")]
    public int FeedbackId { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [StringLength(1000)]
    public string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Feedback Feedback { get; set; }
    public virtual User User { get; set; }
}
