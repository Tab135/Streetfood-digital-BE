using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class FeedbackTagAssociation
    {
        [ForeignKey("Feedback")]
        public int FeedbackId { get; set; }

        [ForeignKey("FeedbackTag")]
        public int TagId { get; set; }

        public virtual Feedback Feedback { get; set; }
        public virtual FeedbackTag FeedbackTag { get; set; }
    }
}