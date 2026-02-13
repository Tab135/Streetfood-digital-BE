using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class Dish
    {
        [Key]
        public int DishId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        public decimal Price { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsSoldOut { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        [ForeignKey("Branch")]
        public int BranchId { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        // Navigation Properties
        public virtual Branch Branch { get; set; }
        public virtual Category Category { get; set; }
        public virtual ICollection<DishTaste> DishTastes { get; set; } = new List<DishTaste>();
        public virtual ICollection<DishDietaryPreference> DishDietaryPreferences { get; set; } = new List<DishDietaryPreference>();
    }
}
