using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Quest
{
    public class CreateQuestTaskDto
    {
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "TargetValue must be greater than 0")]
        public int TargetValue { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string RewardType { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "RewardValue must be greater than 0")]
        public int RewardValue { get; set; }
    }
}
