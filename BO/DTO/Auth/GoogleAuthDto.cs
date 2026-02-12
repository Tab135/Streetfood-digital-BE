using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Auth;

public class GoogleAuthDto
{
    public string? IdToken { get; set; }
    
    public string? AccessToken { get; set; }
}
