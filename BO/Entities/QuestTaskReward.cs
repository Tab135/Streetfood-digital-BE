using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using BO.Enums;

namespace BO.Entities
{
    public class QuestTaskReward
    {
        [Key]
        public int QuestTaskRewardId { get; set; }

        public int QuestTaskId { get; set; }
        [ForeignKey("QuestTaskId")]
        [JsonIgnore]
        public virtual QuestTask QuestTask { get; set; } = null!;

        [Required]
        public QuestRewardType RewardType { get; set; }

        public int RewardValue { get; set; }

        /// <summary>
        /// For VOUCHER: number of voucher instances to grant.
        /// For POINTS: multiplier (points = RewardValue × Quantity).
        /// For BADGE: ignored (badge is unique per user).
        /// </summary>
        public int Quantity { get; set; } = 1;
    }
}
