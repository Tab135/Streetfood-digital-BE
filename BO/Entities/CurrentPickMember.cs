using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class CurrentPickMember
{
    [Key]
    public int CurrentPickMemberId { get; set; }

    [ForeignKey("CurrentPickRoom")]
    public int CurrentPickRoomId { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    public bool IsHost { get; set; } = false;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public virtual CurrentPickRoom CurrentPickRoom { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
