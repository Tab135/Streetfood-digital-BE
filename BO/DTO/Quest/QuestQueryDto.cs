namespace BO.DTO.Quest
{
    public class QuestQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool? IsActive { get; set; }
        public int? CampaignId { get; set; }
    }
}
