using BO.Enums;

namespace BO.DTO.Quest
{
    public class QuestTaskRewardDto
    {
        public int QuestTaskRewardId { get; set; }
        public QuestRewardType RewardType { get; set; }
        public int RewardValue { get; set; }
        public int Quantity { get; set; }
    }
}
