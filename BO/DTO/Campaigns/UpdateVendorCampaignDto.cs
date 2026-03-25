using System;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Campaigns
{
    public class UpdateVendorCampaignDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? TargetSegment { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }
    }
}
