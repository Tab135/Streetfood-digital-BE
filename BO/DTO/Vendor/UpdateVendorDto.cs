using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Vendor
{
    public class UpdateVendorDto
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; }
    }
}
