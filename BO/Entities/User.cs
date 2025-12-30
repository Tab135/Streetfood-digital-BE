using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BO.Entities;

public class User
{
    [Key]
    public int Id { get; set; }
    [StringLength(50)]
    public string? Username { get; set; }
    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    [StringLength(255)]
    public string? Password { get; set; }
    public Role Role { get; set; } = Role.User;
    public DateTime Createdat { get; set; } = DateTime.UtcNow;
    public DateTime Updatedat { get; set; }
    public int Point {  get; set; }
    public bool EmailVerified { get; set; } = false;
    public string? Phone_number {  get; set; }  
    public string? Avatar_url { get; set; }
    public string? Status { get; set; }
}
