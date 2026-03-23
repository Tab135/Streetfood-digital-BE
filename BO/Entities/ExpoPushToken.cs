using System.ComponentModel.DataAnnotations;

namespace BO.Entities;

public class ExpoPushToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Platform { get; set; } = string.Empty; // "ios" or "android"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
