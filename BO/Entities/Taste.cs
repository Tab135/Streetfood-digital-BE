using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.Entities
{
    public class Taste
    {
        [Key]
        public int TasteId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation
        public virtual ICollection<DishTaste> DishTastes { get; set; } = new List<DishTaste>();
    }
}
