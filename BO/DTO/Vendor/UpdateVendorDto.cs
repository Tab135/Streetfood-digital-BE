using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Vendor
{
    public class UpdateVendorDto
    {
        [StringLength(255, ErrorMessage = "Vendor name cannot exceed 255 characters")]
        public string Name { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }

        public string AddressDetail { get; set; }

        [StringLength(255)]
        public string BuildingName { get; set; }

        [StringLength(255)]
        public string Ward { get; set; }

        [StringLength(255)]
        public string City { get; set; }

        public double? Lat { get; set; }

        public double? Long { get; set; }
    }
}
