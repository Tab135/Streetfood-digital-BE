using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BO.Enums;

namespace BO.Entities
{
    public class GhostPin
    {
        [Key]
        public int GhostPinId { get; set; }

        [ForeignKey("Creator")]
        public int CreatorId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        public string Address { get; set; }

        public double Lat { get; set; }

        public double Long { get; set; }

        public GhostPinStatusEnum Status { get; set; } = GhostPinStatusEnum.Pending;

        public string? RejectReason { get; set; }

        [ForeignKey("LinkedBranch")]
        public int? LinkedBranchId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User Creator { get; set; }
        public virtual Branch LinkedBranch { get; set; }
    }
}
