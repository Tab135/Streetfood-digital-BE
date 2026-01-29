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

        [Phone]
        [StringLength(50)]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        public string AddressDetail { get; set; }

        public string? BuildingName { get; set; }

        public string? Ward { get; set; }

        public string? City { get; set; }

        public double Lat { get; set; }

        public double Long { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsVerified { get; set; }

        public double AvgRating { get; set; }

        public bool IsActive { get; set; }

        public bool IsSubscribed { get; set; }

        public virtual User VendorOwner { get; set; }
        //public virtual Tier Tier { get; set; }
    }
}
