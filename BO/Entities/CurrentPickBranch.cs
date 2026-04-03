using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class CurrentPickBranch
{
    [Key]
    public int CurrentPickBranchId { get; set; }

    [ForeignKey("CurrentPickRoom")]
    public int CurrentPickRoomId { get; set; }

    [ForeignKey("Branch")]
    public int BranchId { get; set; }

    [ForeignKey("AddedByUser")]
    public int AddedByUserId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public virtual CurrentPickRoom CurrentPickRoom { get; set; } = null!;

    public virtual Branch Branch { get; set; } = null!;

    public virtual User AddedByUser { get; set; } = null!;
}
