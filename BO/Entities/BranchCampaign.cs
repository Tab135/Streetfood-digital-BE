using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class BranchCampaign
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Campaign")]
        public int CampaignId { get; set; }
        public virtual Campaign Campaign { get; set; }

        [ForeignKey("Branch")]
        public int BranchId { get; set; }
        public virtual Branch Branch { get; set; }


        /// <summary>
        /// Trạng thái hoạt động của branch campaign
        /// </summary>
        // Default must be false so that "joined but unpaid" won't be treated as paid/active.
        public bool IsActive { get; set; } = false;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}