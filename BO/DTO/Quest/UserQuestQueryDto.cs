using BO.Enums;

namespace BO.DTO.Quest
{
    public class UserQuestQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public int? UserId { get; set; }
        public int? QuestId { get; set; }
        public int? CampaignId { get; set; }
        public string? Status { get; set; }
        public bool? IsStandalone { get; set; }
        public bool? IsTierUp { get; set; }
        public string? Search { get; set; }
        public System.DateTime? StartedFrom { get; set; }
        public System.DateTime? StartedTo { get; set; }
        public System.DateTime? CompletedFrom { get; set; }
        public System.DateTime? CompletedTo { get; set; }
    }
}