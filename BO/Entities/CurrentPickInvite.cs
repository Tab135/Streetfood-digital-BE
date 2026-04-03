using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class CurrentPickInvite
{
    [Key]
    public int CurrentPickInviteId { get; set; }

    [ForeignKey("CurrentPickRoom")]
    public int CurrentPickRoomId { get; set; }

    [ForeignKey("InvitedUser")]
    public int InvitedUserId { get; set; }

    [ForeignKey("InvitedByUser")]
    public int InvitedByUserId { get; set; }

    public CurrentPickInviteStatus Status { get; set; } = CurrentPickInviteStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    public virtual CurrentPickRoom CurrentPickRoom { get; set; } = null!;

    public virtual User InvitedUser { get; set; } = null!;

    public virtual User InvitedByUser { get; set; } = null!;
}
