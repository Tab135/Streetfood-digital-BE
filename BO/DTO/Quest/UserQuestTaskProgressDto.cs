using BO.Enums;
using System;
using System.Collections.Generic;

namespace BO.DTO.Quest
{
    public class UserQuestTaskProgressDto
    {
        public int UserQuestTaskId { get; set; }
        public int QuestTaskId { get; set; }
        public QuestTaskType Type { get; set; }
        public int TargetValue { get; set; }
        public string? Description { get; set; }
        public List<QuestTaskRewardDto> Rewards { get; set; } = new();
        public int CurrentValue { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool RewardClaimed { get; set; }
    }
}
