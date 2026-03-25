using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class CampaignImage
    {
        [Key]
        public int CampaignImageId { get; set; }

        [ForeignKey("Campaign")]
        public int CampaignId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        // Navigation property
        public virtual Campaign Campaign { get; set; }
    }
}
