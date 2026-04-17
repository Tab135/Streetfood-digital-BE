using BO.DTO.Users;
using System;

namespace BO.DTO.Quest
{
    public class UserQuestResponseDto
    {
        public int UserQuestId { get; set; }
        public int UserId { get; set; }
        public UserProfileDto User { get; set; } = new();
        public int QuestId { get; set; }
        public string QuestTitle { get; set; } = string.Empty;
        public string? QuestImageUrl { get; set; }
        public int? CampaignId { get; set; }
        public bool IsStandalone { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
    }
}