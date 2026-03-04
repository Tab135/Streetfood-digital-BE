using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BO.DTO.Payments;
namespace BO.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        /// <summary>Branch this payment is for (vendor subscription flow)</summary>
        public int? BranchId { get; set; }

        [Required]
        public long OrderCode { get; set; }

        [Required]
        public int Amount { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string? Status { get; set; } // PENDING, PAID, CANCELLED

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        [StringLength(500)]
        public string? PaymentLinkId { get; set; }

        [StringLength(50)]
        public string? TransactionCode { get; set; }

        // PayOS specific fields
        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        [StringLength(500)]
        public string? CheckoutUrl { get; set; }

        // Navigation the user 
        public User? User { get; set; }
    }
}
