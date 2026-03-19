using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Branch
{
    public class CreateUserBranchRequest
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = null!;

        [Required]
        public string AddressDetail { get; set; } = null!;

        [Required]
        public string Ward { get; set; } = null!;

        [Required]
        public string City { get; set; } = null!;

        [Required]
        public double Lat { get; set; }

        [Required]
        public double Long { get; set; }
    }
}
