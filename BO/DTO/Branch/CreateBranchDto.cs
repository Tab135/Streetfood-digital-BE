using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class CreateBranchDto
    {
        [Required(ErrorMessage = "Branch name is required")]
        [StringLength(255, ErrorMessage = "Branch name cannot exceed 255 characters")]
        public string Name { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Address detail is required")]
        public string AddressDetail { get; set; }

        [StringLength(255)]
        public string BuildingName { get; set; }

        [StringLength(255)]
        public string Ward { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(255)]
        public string City { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        public double Lat { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        public double Long { get; set; }
    }
}
