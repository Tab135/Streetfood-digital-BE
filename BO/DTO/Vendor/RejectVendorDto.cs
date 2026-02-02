using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Vendor
{
    public class RejectVendorDto
    {
        [Required(ErrorMessage = "Rejection reason is required")]
        [StringLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters")]
        public string RejectionReason { get; set; }
    }
}
