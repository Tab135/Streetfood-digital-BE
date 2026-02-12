using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class FeedbackImage
    {
        [Key]
        public int FeedbackImageId { get; set; }

        [ForeignKey("Feedback")]
        public int FeedbackId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        public virtual Feedback Feedback { get; set; }
    }
}