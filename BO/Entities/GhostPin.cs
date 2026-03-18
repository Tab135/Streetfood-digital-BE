using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string AddressDetail { get; set; }

        [StringLength(255)]
        public string? Ward { get; set; }

        [Required]
        [StringLength(255)]
        public string City { get; set; }

        public double Lat { get; set; }

        public double Long { get; set; }

        public bool IsVerified { get; set; } = false;

        public double AvgRating { get; set; } = 0;
        public int TotalReviewCount { get; set; } = 0;
        public int TotalRatingSum { get; set; } = 0;

        public int BatchReviewCount { get; set; } = 0;
        public int BatchRatingSum { get; set; } = 0;

        public int TierId { get; set; } = 2; // Default to Silver (2)
        public DateTime? LastTierResetAt { get; set; }

        public string? RejectReason { get; set; }

        [ForeignKey("LinkedBranch")]
        public int? LinkedBranchId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User Creator { get; set; }
        public virtual Branch LinkedBranch { get; set; }
        
        [ForeignKey("TierId")]
        public virtual Tier Tier { get; set; }
    }
}
