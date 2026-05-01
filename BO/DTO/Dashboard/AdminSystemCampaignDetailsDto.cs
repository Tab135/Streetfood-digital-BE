using System.Collections.Generic;

namespace BO.DTO.Dashboard
{
    public class AdminSystemCampaignDetailsDto
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        
        public int TotalBranchesJoined { get; set; }
        public int TotalOrders { get; set; }
        
        public List<AdminSystemCampaignQuestDto> Quests { get; set; } = new List<AdminSystemCampaignQuestDto>();
        public List<AdminSystemCampaignBranchOrderDto> BranchOrders { get; set; } = new List<AdminSystemCampaignBranchOrderDto>();
        public List<AdminSystemCampaignVoucherDto> Vouchers { get; set; } = new List<AdminSystemCampaignVoucherDto>();
        public List<AdminSystemCampaignOrderDto> CampaignOrders { get; set; } = new List<AdminSystemCampaignOrderDto>();
    }

    public class AdminSystemCampaignQuestDto
    {
        public int QuestId { get; set; }
        public string QuestTitle { get; set; } = string.Empty;
        public int TotalUsersDoing { get; set; }
        public int UsersCurrentlyDoing { get; set; }
        public int UsersFinished { get; set; }
    }

    public class AdminSystemCampaignBranchOrderDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
    }

    public class AdminSystemCampaignVoucherDto
    {
        public int VoucherId { get; set; }
        public string VoucherName { get; set; } = string.Empty;
        public int TotalUsed { get; set; }
    }

    public class AdminSystemCampaignOrderDto
    {
        public int OrderId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string VoucherName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }
}
