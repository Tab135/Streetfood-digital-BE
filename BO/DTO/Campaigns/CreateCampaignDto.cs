using System;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Campaigns
{
    public class CreateCampaignDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TargetSegment { get; set; }
        
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Active";
    }
}
