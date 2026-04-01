using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.Entities
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        // Navigation
        public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    }
}
