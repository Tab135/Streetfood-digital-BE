using System.Collections.Generic;

namespace BO.DTO.Campaigns
{
    public class VendorCampaignBranchesResponseDto
    {
        public int CampaignId { get; set; }
        public List<int> BranchIds { get; set; } = new();
    }
}
