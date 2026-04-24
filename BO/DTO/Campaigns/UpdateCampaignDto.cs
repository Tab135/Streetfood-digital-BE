using System;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Campaigns
{
    public class UpdateCampaignDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? TargetSegment { get; set; }

        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool? IsActive { get; set; }
        public int? JoinFee { get; set; }
        public int? ExpectedBranchJoin { get; set; }
    }
}

