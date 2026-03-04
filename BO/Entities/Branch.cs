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
        public int VendorId { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }

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

        public bool IsActive { get; set; }

        public bool IsSubscribed { get; set; }

        /// <summary>Date when the paid subscription expires (null = never paid)</summary>
        public DateTime? SubscriptionExpiresAt { get; set; }

        // Navigation properties
        public virtual Vendor Vendor { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<WorkSchedule> WorkSchedules { get; set; }
        public virtual ICollection<DayOff> DayOffs { get; set; }
        public virtual ICollection<BranchImage> BranchImages { get; set; }
        public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    }
}
