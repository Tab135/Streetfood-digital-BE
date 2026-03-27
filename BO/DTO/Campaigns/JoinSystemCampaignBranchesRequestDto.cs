using System.Collections.Generic;

namespace BO.DTO.Campaigns
{
    public class JoinSystemCampaignBranchesRequestDto
    {
        public List<int> BranchIds { get; set; } = new();
    }
}

