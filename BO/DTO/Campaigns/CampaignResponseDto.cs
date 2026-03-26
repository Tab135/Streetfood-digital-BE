using System;
using System.Text.Json.Serialization;

namespace BO.DTO.Campaigns
{
    public class CampaignResponseDto
    {
        public int CampaignId { get; set; }
        public int? CreatedByBranchId { get; set; }
        public int? CreatedByVendorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TargetSegment { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? RegistrationStartDate { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? RegistrationEndDate { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsSystemCampaign => CreatedByBranchId == null && CreatedByVendorId == null;
    }
}
