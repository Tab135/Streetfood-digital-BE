using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO.Entities
{
    public class VendorImage
    {
        [Key]
        [Column("venderImageId")]
        public int VendorImageId { get; set; }

        [ForeignKey("Vendor")]
        public int VendorId { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        public virtual Vendor Vendor { get; set; }
    }
}
