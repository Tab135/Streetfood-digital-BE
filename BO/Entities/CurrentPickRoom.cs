using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class CurrentPickRoom
{
    [Key]
    public int CurrentPickRoomId { get; set; }

    [Required]
    [StringLength(12)]
    public string RoomCode { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Title { get; set; }

    [ForeignKey("HostUser")]
    public int HostUserId { get; set; }

    public bool IsFinalized { get; set; } = false;

    [ForeignKey("FinalizedBranch")]
    public int? FinalizedBranchId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinalizedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual User HostUser { get; set; } = null!;

    public virtual Branch? FinalizedBranch { get; set; }

    public virtual ICollection<CurrentPickMember> Members { get; set; } = new List<CurrentPickMember>();

    public virtual ICollection<CurrentPickBranch> Branches { get; set; } = new List<CurrentPickBranch>();

    public virtual ICollection<CurrentPickVote> Votes { get; set; } = new List<CurrentPickVote>();
}
