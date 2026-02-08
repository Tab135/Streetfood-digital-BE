using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Auth;

public class FacebookAuthDto
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}
