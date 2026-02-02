using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BO.Entities
{
    public class VendorRegisterRequest
    {
        [Key]
        public int VendorRegisterRequestId { get; set; } 
        [ForeignKey("VendorId")]
        public int VendorId { get; set; }
        public RegisterVendorStatusEnum Status { get; set; }
        public string? rejectReason { get; set; }
        public string LicenseUrl { get; set; }
        public User? processedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
    }
}
