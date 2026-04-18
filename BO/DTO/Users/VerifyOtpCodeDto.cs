using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Users;

public class VerifyOtpCodeDto
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
}
