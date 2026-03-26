using BO.Enums;

namespace BO.DTO.Quest
{
    public class QuestTaskResponseDto
    {
        public int QuestTaskId { get; set; }
        public QuestTaskType Type { get; set; }
        public int TargetValue { get; set; }
        public string? Description { get; set; }
        public QuestRewardType RewardType { get; set; }
        public int RewardValue { get; set; }
    }
}
