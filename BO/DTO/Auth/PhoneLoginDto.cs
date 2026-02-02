using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Auth
{
    public class PhoneLoginDto
    {
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
