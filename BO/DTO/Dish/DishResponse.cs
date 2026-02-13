using System;
using System.Collections.Generic;

namespace BO.DTO.Dish
{
    public class DishResponse
    {
        public int DishId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSoldOut { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Foreign Key IDs
        public int BranchId { get; set; }
        public int CategoryId { get; set; }

        // Flattened data for FE display
        public string CategoryName { get; set; }
        public List<string> TasteNames { get; set; } = new List<string>();
        public List<string> DietaryPreferenceNames { get; set; } = new List<string>();
    }
}
