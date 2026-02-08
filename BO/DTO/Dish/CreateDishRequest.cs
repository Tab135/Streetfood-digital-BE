using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Dish
{
    public class CreateDishRequest
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value")]
        public decimal Price { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public int BranchId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        /// <summary>
        /// List of TasteIds to associate with this dish (many-to-many)
        /// </summary>
        public List<int> TasteIds { get; set; } = new List<int>();

        /// <summary>
        /// List of DietaryPreferenceIds to associate with this dish (many-to-many via DishDietaryPreference)
        /// </summary>
        public List<int> DietaryPreferenceIds { get; set; } = new List<int>();
    }
}
