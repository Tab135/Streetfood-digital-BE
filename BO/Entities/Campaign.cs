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

        public int? CreatedByBranchId { get; set; }
        [ForeignKey("CreatedByBranchId")]
        public virtual Branch? CreatedByBranch { get; set; }

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

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Draft, Active, Inactive
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public virtual ICollection<BranchCampaign> BranchCampaigns { get; set; } = new List<BranchCampaign>();
        public virtual ICollection<CampaignImage> CampaignImages { get; set; } = new List<CampaignImage>();
    }
}
