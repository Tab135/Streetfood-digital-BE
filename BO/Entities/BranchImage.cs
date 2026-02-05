using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class BranchImage
    {
        [Key]
        public int BranchImageId { get; set; }

        [ForeignKey("Branch")]
        public int BranchId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        // Navigation property
        public virtual Branch Branch { get; set; }
    }
}
