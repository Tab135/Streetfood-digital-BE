using System.Collections.Generic;

namespace BO.DTO.Campaigns
{
    public class VendorJoinSystemCampaignPaymentResponseDto
    {
        public VendorJoinSystemCampaignPaymentInfoDto? Payment { get; set; }
        public List<VendorJoinSystemCampaignBranchStatusDto> Branches { get; set; } = new();
    }

    public class VendorJoinSystemCampaignPaymentInfoDto
    {
        public string? PaymentUrl { get; set; }
        public long? OrderCode { get; set; }
        public string? PaymentLinkId { get; set; }
    }

    public class VendorJoinSystemCampaignBranchStatusDto
    {
        public int BranchId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

