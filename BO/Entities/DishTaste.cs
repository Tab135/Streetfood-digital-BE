using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class DishTaste
    {
        [Key]
        public int DishTasteId { get; set; }

        [ForeignKey("Dish")]
        public int DishId { get; set; }

        [ForeignKey("Taste")]
        public int TasteId { get; set; }

        // Navigation Properties
        public virtual Dish Dish { get; set; }
        public virtual Taste Taste { get; set; }
    }
}
