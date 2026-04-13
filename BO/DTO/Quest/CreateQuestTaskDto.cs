using BO.Enums;
using System.Collections.Generic;
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
        [MinLength(1, ErrorMessage = "At least one reward is required per task")]
        public List<CreateQuestTaskRewardDto> Rewards { get; set; } = new();
    }
}
