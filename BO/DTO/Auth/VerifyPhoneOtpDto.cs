using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Auth
{
    public class VerifyPhoneOtpDto
    {
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }
}
