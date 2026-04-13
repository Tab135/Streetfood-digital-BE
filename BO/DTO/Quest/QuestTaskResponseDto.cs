using BO.Enums;
using System.Collections.Generic;

namespace BO.DTO.Quest
{
    public class QuestTaskResponseDto
    {
        public int QuestTaskId { get; set; }
        public int QuestId { get; set; }
        public QuestTaskType Type { get; set; }
        public int TargetValue { get; set; }
        public string? Description { get; set; }
        public List<QuestTaskRewardDto> Rewards { get; set; } = new();
    }
}
