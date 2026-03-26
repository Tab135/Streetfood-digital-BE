using BO.Enums;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Quest
{
    public class CreateQuestTaskDto
    {
        [Required]
        public QuestTaskType Type { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "TargetValue must be greater than 0")]
        public int TargetValue { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public QuestRewardType RewardType { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "RewardValue must be greater than 0")]
        public int RewardValue { get; set; }
    }
}
