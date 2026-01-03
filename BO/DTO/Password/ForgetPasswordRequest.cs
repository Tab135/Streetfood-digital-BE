using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Password;

public class ForgetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

}
