using BO.Enums;

namespace BO.DTO.Quest
{
    public class UserQuestTaskQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public int? UserId { get; set; }
        public int? UserQuestId { get; set; }
        public int? QuestTaskId { get; set; }
        public string? Status { get; set; }
        public QuestTaskType? Type { get; set; }
        public bool? IsCompleted { get; set; }
        public bool? RewardClaimed { get; set; }
        public string? Search { get; set; }
        public System.DateTime? CompletedFrom { get; set; }
        public System.DateTime? CompletedTo { get; set; }
    }
}