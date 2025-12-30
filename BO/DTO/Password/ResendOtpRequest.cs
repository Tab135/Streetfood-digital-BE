using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Password;

public class ResendOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;
} 
