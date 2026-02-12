using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("Branch")]
        public int BranchId { get; set; }

        // [ForeignKey("Dish")]
        // public int? DishId { get; set; }

        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        public virtual User User { get; set; }
        public virtual Branch Branch { get; set; }
        public virtual ICollection<FeedbackImage> FeedbackImages { get; set; } = new List<FeedbackImage>();
        public virtual ICollection<FeedbackTagAssociation> FeedbackTagAssociations { get; set; } = new List<FeedbackTagAssociation>();
    }
}