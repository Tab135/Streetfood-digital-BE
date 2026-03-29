using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BO.Entities
{
    public class UserQuestTask
    {
        [Key]
        public int UserQuestTaskId { get; set; }

        public int UserQuestId { get; set; }
        [ForeignKey("UserQuestId")]
        [JsonIgnore]
        public virtual UserQuest UserQuest { get; set; } = null!;

        public int QuestTaskId { get; set; }
        [ForeignKey("QuestTaskId")]
        public virtual QuestTask QuestTask { get; set; } = null!;

        public int CurrentValue { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        public bool RewardClaimed { get; set; } = false;
    }
}
