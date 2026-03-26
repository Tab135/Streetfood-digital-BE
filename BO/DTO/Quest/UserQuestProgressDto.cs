using System;
using System.Collections.Generic;

namespace BO.DTO.Quest
{
    public class UserQuestProgressDto
    {
        public int UserQuestId { get; set; }
        public int QuestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? CampaignId { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public List<UserQuestTaskProgressDto> Tasks { get; set; } = new();
    }
}
