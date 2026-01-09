using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
    public DateTime UpdatedAt { get; set; }
    public int Point {  get; set; }
    public bool EmailVerified { get; set; } = false;
    public string? PhoneNumber {  get; set; }  
    public string? AvatarUrl { get; set; }
    public string? Status { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
