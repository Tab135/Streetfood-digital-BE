using BO.DTO.Users;
using BO.Enums;
using System;
using System.Collections.Generic;

namespace BO.DTO.Quest
{
    public class UserQuestTaskResponseDto
    {
        public int UserQuestTaskId { get; set; }
        public int UserQuestId { get; set; }
        public int UserId { get; set; }
        public UserProfileDto User { get; set; } = new();
        public int QuestId { get; set; }
        public string QuestTitle { get; set; } = string.Empty;
        public int QuestTaskId { get; set; }
        public QuestTaskType Type { get; set; }
        public int TargetValue { get; set; }
        public string? Description { get; set; }
        public List<QuestTaskRewardDto> Rewards { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public int CurrentValue { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool RewardClaimed { get; set; }
    }
}