using System;
using System.ComponentModel.DataAnnotations;

namespace BO.Entities;

public class OtpVerify
{
    [Key]
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiredAt { get; set; }

    public bool IsUsed { get; set; } = false;
}
