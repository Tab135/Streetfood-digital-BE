using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Dish
{
    public class UpdateDishRequest
    {
        [StringLength(255)]
        public string? Name { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value")]
        public decimal? Price { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsSignature { get; set; }

        public int? CategoryId { get; set; }

        /// <summary>
        /// If provided, replaces all existing TasteIds for this dish
        /// </summary>
        public List<int>? TasteIds { get; set; }

    }
}
