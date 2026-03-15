using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class BranchDish
    {
        [ForeignKey("Branch")]
        public int BranchId { get; set; }

        [ForeignKey("Dish")]
        public int DishId { get; set; }

        // Branch-level availability for this vendor-owned dish.
        public bool IsAvailable { get; set; } = true;

        public System.DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Branch Branch { get; set; }
        public virtual Dish Dish { get; set; }
    }
}
