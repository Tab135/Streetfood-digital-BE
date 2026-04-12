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
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RequiredTierId { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsRegisterable { get; set; }
        public bool IsUpdateable { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSystemCampaign => CreatedByBranchId == null && CreatedByVendorId == null;
    }
}
