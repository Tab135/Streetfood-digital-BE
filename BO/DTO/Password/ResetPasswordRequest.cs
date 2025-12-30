using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Password;

public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
} 
