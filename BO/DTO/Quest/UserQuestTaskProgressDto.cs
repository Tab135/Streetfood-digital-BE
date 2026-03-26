using BO.Enums;
using System;

namespace BO.DTO.Quest
{
    public class UserQuestTaskProgressDto
    {
        public int UserQuestTaskId { get; set; }
        public int QuestTaskId { get; set; }
        public QuestTaskType Type { get; set; }
        public int TargetValue { get; set; }
        public string? Description { get; set; }
        public QuestRewardType RewardType { get; set; }
        public int RewardValue { get; set; }
        public int CurrentValue { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool RewardClaimed { get; set; }
    }
}
