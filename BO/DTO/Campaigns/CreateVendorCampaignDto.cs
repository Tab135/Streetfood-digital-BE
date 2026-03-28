using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Campaigns
{
    public class CreateVendorCampaignDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TargetSegment { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Null or omitted: attach every eligible branch (tier + subscription). Non-empty: only these branches (must belong to the vendor).
        /// </summary>
        public List<int>? BranchIds { get; set; }
    }
}
