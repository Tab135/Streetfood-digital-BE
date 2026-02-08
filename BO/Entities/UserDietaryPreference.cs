using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO.Entities
{
    public class UserDietaryPreference
    {
        [Key]
        [Column("userDietaryPreferencesId")]
        public int UserDietaryPreferenceId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("DietaryPreference")]
        [Column("dietaryPreferenceId")]
        public int DietaryPreferenceId { get; set; }

        public virtual User User { get; set; }
        public virtual DietaryPreference DietaryPreference { get; set; }
    }
}
