using System;
using System.Collections.Generic;

namespace BO.DTO.Campaigns
{
    public class BranchCampaignInfoDto
    {
        public int CampaignId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsWorking { get; set; }
        public List<CampaignVoucherInfoDto> Vouchers { get; set; } = new();
    }

    public class CampaignVoucherInfoDto
    {
        public int VoucherId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal MinAmountRequired { get; set; }
        public decimal? MaxDiscountValue { get; set; }
        public int Quantity { get; set; }
        public int UsedQuantity { get; set; }
        public int Remain => Quantity - UsedQuantity;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
    }
}
