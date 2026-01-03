using System;
using System.ComponentModel.DataAnnotations;

namespace BO.Entities;

public class Badge
{
    [Key]
    public int BadgeId { get; set; }

    [StringLength(100)]
    public string BadgeName { get; set; } = string.Empty;

    public int PointToGet { get; set; }

    [StringLength(500)]
    public string IconUrl { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }
}