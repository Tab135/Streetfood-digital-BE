using System.ComponentModel.DataAnnotations;
using BO.Entities;

namespace BO.DTO.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public User? User { get; set; }
}
