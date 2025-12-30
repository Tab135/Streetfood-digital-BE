using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Auth;

public class RegisterDto
{
    [Required]
    [StringLength(100, MinimumLength = 5)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
