using BO.DTO.Branch;

namespace BO.DTO.Campaigns
{
    public class CampaignBranchResponseDto : BranchPublicDto
    {
        public double FinalScore { get; set; }
        public double? DistanceKm { get; set; }
    }
}