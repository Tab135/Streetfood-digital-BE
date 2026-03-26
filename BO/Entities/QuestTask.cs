using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using BO.Enums;

namespace BO.Entities
{
    public class QuestTask
    {
        [Key]
        public int QuestTaskId { get; set; }

        public int QuestId { get; set; }
        [ForeignKey("QuestId")]
        [JsonIgnore]
        public virtual Quest Quest { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public int TargetValue { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string RewardType { get; set; } = string.Empty;

        public int RewardValue { get; set; }

        [JsonIgnore]
        public virtual ICollection<UserQuestTask> UserQuestTasks { get; set; } = new List<UserQuestTask>();
    }
}
