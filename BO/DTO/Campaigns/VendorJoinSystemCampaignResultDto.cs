using System.Collections.Generic;

namespace BO.DTO.Campaigns
{
    public class VendorJoinSystemCampaignResultDto
    {
        public List<VendorJoinSystemCampaignBranchDto> Branches { get; set; } = new();
    }

    public class VendorJoinSystemCampaignBranchDto
    {
        public int BranchId { get; set; }
        public string Status { get; set; } = string.Empty; // ALREADY_JOINED, PAYMENT_REQUIRED
        public string? PaymentUrl { get; set; }
        public long? OrderCode { get; set; }
        public string? PaymentLinkId { get; set; }
    }
}