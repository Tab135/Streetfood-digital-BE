using BO.DTO.Branch;
using System.Collections.Generic;

namespace BO.DTO.Campaigns
{
    public class CampaignBranchResponseDto : BranchPublicDto
    {
        public double FinalScore { get; set; }
        public double? DistanceKm { get; set; }
        public List<BranchCampaignInfoDto> Campaigns { get; set; } = new();
    }
}