using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Auth;

public class GoogleAuthDto
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
