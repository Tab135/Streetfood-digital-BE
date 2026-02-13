using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO.Entities
{
    public class DietaryPreference
    {
        [Key]
        [Column("dietaryPreferenceId")]
        public int DietaryPreferenceId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }


        public virtual ICollection<UserDietaryPreference> UserPreferences { get; set; }
        public virtual ICollection<DishDietaryPreference> DishDietaryPreferences { get; set; } = new List<DishDietaryPreference>();
    }
}
