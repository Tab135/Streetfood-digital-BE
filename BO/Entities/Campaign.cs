using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class Campaign
    {
        [Key]
        public int CampaignId { get; set; }

        public int? CreatedByVendorId { get; set; }
        [ForeignKey("CreatedByVendorId")]
        public virtual Vendor? CreatedByVendor { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(100)]
        public string? TargetSegment { get; set; }

        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }

        public int? RequiredTierId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        /// <summary>
        /// Trạng thái hoạt động của campaign
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        public bool IsRegisterable { get; set; } = false;

        public int JoinFee { get; set; }

        public virtual ICollection<BranchCampaign> BranchCampaigns { get; set; } = new List<BranchCampaign>();
        public string? ImageUrl { get; set; }
    }
}
