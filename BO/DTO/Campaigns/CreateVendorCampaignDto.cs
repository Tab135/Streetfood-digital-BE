using System;
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
    }
}
