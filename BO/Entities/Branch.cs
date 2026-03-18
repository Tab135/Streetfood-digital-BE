using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class Branch
    {
        [Key]
        public int BranchId { get; set; }

        [ForeignKey("Vendor")]
        public int? VendorId { get; set; }

        [ForeignKey("Manager")]
        public int? ManagerId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Phone]
        [StringLength(50)]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        public string AddressDetail { get; set; }

        [StringLength(255)]
        public string? Ward { get; set; }

        [Required]
        [StringLength(255)]
        public string City { get; set; }

        public double Lat { get; set; }

        public double Long { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsVerified { get; set; }

        public double AvgRating { get; set; }

        public int TotalReviewCount { get; set; } = 0;
        public int TotalRatingSum { get; set; } = 0;

        public bool IsActive { get; set; }

        public bool IsSubscribed { get; set; }

        /// <summary>Date when the paid subscription expires (null = never paid)</summary>
        public DateTime? SubscriptionExpiresAt { get; set; }

        public DateTime? LastTierResetAt { get; set; }

        // --- Tier Configuration ---
        public int TierId { get; set; } = 2; // Default to Silver (2)

        public int BatchReviewCount { get; set; } = 0;

        public int BatchRatingSum { get; set; } = 0;

        // Navigation properties
        [ForeignKey("TierId")]
        public virtual Tier Tier { get; set; }
        
        public virtual Vendor Vendor { get; set; }
        public virtual User Manager { get; set; }
        public virtual ICollection<WorkSchedule> WorkSchedules { get; set; }
        public virtual ICollection<DayOff> DayOffs { get; set; }
        public virtual ICollection<BranchImage> BranchImages { get; set; }
        public virtual ICollection<BranchDish> BranchDishes { get; set; } = new List<BranchDish>();
    }
}
