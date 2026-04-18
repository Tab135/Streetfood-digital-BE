using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.Entities;

public class User
{
    [Key]
    public int Id { get; set; }
    [StringLength(50)]
    public string? UserName { get; set; }
    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    [StringLength(255)]
    public string? Password { get; set; }
    public Role Role { get; set; } = Role.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Point {  get; set; }
    public int XP { get; set; } = 0;
    
    public int? TierId { get; set; } = 2; // Default Customer Tier = Silver
    [ForeignKey("TierId")]
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual Tier? Tier { get; set; }

    [NotMapped]
    public int? NextTierXP { get; set; }

    public bool EmailVerified { get; set; } = false;

    [StringLength(20)]
    public string? PhoneNumber {  get; set; }

    public bool PhoneNumberVerified { get; set; } = false;

    // Avatar URLs from providers can be very long; map to text to avoid truncation
    [Column(TypeName = "text")]
    public string? AvatarUrl { get; set; }

    [StringLength(100)]
    public string? Status { get; set; }

    [StringLength(100)]
    public string FirstName { get; set; }

    [StringLength(100)]
    public string LastName { get; set; }

    public virtual ICollection<UserDietaryPreference> DietaryPreferences { get; set; }

    // Setup flags
    public bool UserInfoSetup { get; set; } = false;
    public bool DietarySetup { get; set; } = false;

    public decimal MoneyBalance { get; set; } = 0m;
}

