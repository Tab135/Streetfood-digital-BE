using BO.Enums;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Quest
{
    public class CreateQuestTaskRewardDto
    {
        [Required]
        public QuestRewardType RewardType { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "RewardValue must be greater than 0")]
        public int RewardValue { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;
    }
}
