using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BO.Entities
{
    public class UserQuest
    {
        [Key]
        public int UserQuestId { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        [JsonIgnore]
        public virtual User User { get; set; } = null!;

        public int QuestId { get; set; }
        [ForeignKey("QuestId")]
        [JsonIgnore]
        public virtual Quest Quest { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "IN_PROGRESS";

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public virtual ICollection<UserQuestTask> UserQuestTasks { get; set; } = new List<UserQuestTask>();
    }
}
