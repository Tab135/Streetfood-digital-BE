using System;

namespace BO.DTO.Campaigns
{
    public class CampaignQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public int? VendorId { get; set; }
        public bool? IsSystem { get; set; }
    }
}
