using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Users;

public class UpdateUserProfileDto
{
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters long")]
    [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
    public string? Username { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Status { get; set; }
} 
