using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
