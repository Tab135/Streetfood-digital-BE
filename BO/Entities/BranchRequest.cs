using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities
{
    public class BranchRequest
    {
        [Key]
        public int BranchRequestId { get; set; }

        [ForeignKey("Branch")]
        public int BranchId { get; set; }

        public string? LicenseUrl { get; set; }

        [Required]
        public int Type { get; set; }

        [Required]
        public RegisterVendorStatusEnum Status { get; set; }

        public int? VerifiedBy { get; set; }

        public string? RejectReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Branch Branch { get; set; }
    }
}



