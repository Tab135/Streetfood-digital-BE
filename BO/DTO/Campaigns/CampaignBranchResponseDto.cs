using BO.DTO.Branch;
using System;
using System.Collections.Generic;

namespace BO.DTO.Campaigns
{
    public class CampaignBranchResponseDto : BranchResponseDto
    {
        public double FinalScore { get; set; }
        public double? DistanceKm { get; set; }
        public List<BranchCampaignInfoDto> Campaigns { get; set; } = new();
    }
}