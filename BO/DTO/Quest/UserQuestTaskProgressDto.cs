using System;

namespace BO.DTO.Quest
{
    public class UserQuestTaskProgressDto
    {
        public int UserQuestTaskId { get; set; }
        public int QuestTaskId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int TargetValue { get; set; }
        public string? Description { get; set; }
        public string RewardType { get; set; } = string.Empty;
        public int RewardValue { get; set; }
        public int CurrentValue { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool RewardClaimed { get; set; }
    }
}
