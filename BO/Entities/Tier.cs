using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.Entities
{
    public class Tier
    {
        [Key]
        public int TierId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public double Weight { get; set; }

        // Navigation property
        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }
}