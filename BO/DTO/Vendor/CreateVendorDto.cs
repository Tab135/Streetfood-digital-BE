using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Vendor
{
    /// <summary>
    /// DTO for creating a vendor with its default branch.
    /// The default branch will automatically use the vendor name.
    /// </summary>
    public class CreateVendorDto
    {
        [Required(ErrorMessage = "Vendor name is required")]
        [StringLength(255, ErrorMessage = "Vendor name cannot exceed 255 characters")]
        public string Name { get; set; }

        [StringLength(255, ErrorMessage = "Branch name cannot exceed 255 characters")]
        public string BranchName { get; set; }

        [Required(ErrorMessage = "Dietary preference IDs are required")]
        [MinLength(1, ErrorMessage = "At least one dietary preference ID is required")]
        public required List<int> DietaryPreferenceIds { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Address detail is required")]
        [StringLength(255, ErrorMessage = "Address detail cannot exceed 255 characters")]
        public string AddressDetail { get; set; }

        [StringLength(255, ErrorMessage = "Ward cannot exceed 255 characters")]
        public string Ward { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(255, ErrorMessage = "City cannot exceed 255 characters")]
        public string City { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        public double Lat { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        public double Long { get; set; }
    }
}
