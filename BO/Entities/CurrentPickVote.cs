using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class CurrentPickVote
{
    [Key]
    public int CurrentPickVoteId { get; set; }

    [ForeignKey("CurrentPickRoom")]
    public int CurrentPickRoomId { get; set; }

    [ForeignKey("Branch")]
    public int BranchId { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    public virtual CurrentPickRoom CurrentPickRoom { get; set; } = null!;

    public virtual Branch Branch { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
