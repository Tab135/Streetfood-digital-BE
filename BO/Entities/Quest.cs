using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class Quest
    {
        [Key]
        public int QuestId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CampaignId { get; set; }
        [ForeignKey("CampaignId")]
        public virtual Campaign? Campaign { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<QuestTask> QuestTasks { get; set; } = new List<QuestTask>();
        public virtual ICollection<UserQuest> UserQuests { get; set; } = new List<UserQuest>();
    }
}
