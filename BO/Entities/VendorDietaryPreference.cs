using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class VendorDietaryPreference
    {
        [Key]
        public int VendorDietaryPreferenceId { get; set; }

        [ForeignKey("Vendor")]
        public int VendorId { get; set; }

        [ForeignKey("DietaryPreference")]
        public int DietaryPreferenceId { get; set; }

        // Navigation Properties
        public virtual Vendor Vendor { get; set; }
        public virtual DietaryPreference DietaryPreference { get; set; }
    }
}
