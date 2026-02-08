using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class DishDietaryPreference
    {
        [Key]
        public int DishDietaryPreferenceId { get; set; }

        [ForeignKey("DietaryPreference")]
        public int DietaryPreferenceId { get; set; }

        [ForeignKey("Dish")]
        public int DishId { get; set; }

        // Navigation Properties
        public virtual DietaryPreference DietaryPreference { get; set; }
        public virtual Dish Dish { get; set; }
    }
}
