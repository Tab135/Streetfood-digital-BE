using System;
using System.Collections.Generic;

namespace BO.DTO.Quest
{
    public class QuestResponseDto
    {
        public int QuestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int? CampaignId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TaskCount { get; set; }
        public List<QuestTaskResponseDto> Tasks { get; set; } = new();
    }
}
