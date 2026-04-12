using System;
using System.Collections.Generic;

namespace BO.DTO.Campaigns
{
    public class SystemCampaignDetailDto
    {
        public int CampaignId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TargetSegment { get; set; }
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public int? RequiredTierId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsRegisterable { get; set; }
        public string? ImageUrl { get; set; }
        public List<int> JoinableBranch { get; set; } = new List<int>();
    }
}