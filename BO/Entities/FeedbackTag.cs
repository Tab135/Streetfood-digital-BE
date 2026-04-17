using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.Entities
{
    public class FeedbackTag
    {
        [Key]
        public int TagId { get; set; }

        [Required]
        [StringLength(100)]
        public string TagName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<FeedbackTagAssociation> FeedbackTagAssociations { get; set; } = new List<FeedbackTagAssociation>();
    }
}