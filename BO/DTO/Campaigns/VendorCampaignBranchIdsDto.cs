using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Campaigns
{
    public class VendorCampaignBranchIdsDto
    {
        [Required]
        public List<int> BranchIds { get; set; } = new();
    }
}
