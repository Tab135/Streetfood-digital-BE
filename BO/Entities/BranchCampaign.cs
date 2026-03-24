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
        /// pending, paid, active, rejected
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}