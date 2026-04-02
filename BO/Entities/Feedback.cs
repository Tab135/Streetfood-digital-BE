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

        [ForeignKey("Dish")]
        public int? DishId { get; set; }

        [ForeignKey("Order")]
        public int? OrderId { get; set; }

        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public int? FeedbackXP { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        public virtual User User { get; set; }
        public virtual Branch Branch { get; set; }
        public virtual Dish? Dish { get; set; }
        public virtual Order? Order { get; set; }
        public virtual VendorReply? VendorReply { get; set; }
        public virtual ICollection<FeedbackVote> Votes { get; set; } = new List<FeedbackVote>();
        public virtual ICollection<FeedbackImage> FeedbackImages { get; set; } = new List<FeedbackImage>();
        public virtual ICollection<FeedbackTagAssociation> FeedbackTagAssociations { get; set; } = new List<FeedbackTagAssociation>();
    }
}