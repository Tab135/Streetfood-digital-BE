using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO.Entities
{
    public class Vendor
    {
        [Key]
        public int VendorId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }


        //[ForeignKey("Tier")]
        //public int TierId { get; set; }

        [Required]
        [StringLength(255)] 
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; }

        // Navigation properties
        public virtual User VendorOwner { get; set; }
        public virtual ICollection<Branch> Branches { get; set; }
        //public virtual Tier Tier { get; set; }
    }
}
