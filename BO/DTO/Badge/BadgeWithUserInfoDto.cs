using System;

namespace BO.DTO.Badge;

public class BadgeWithUserInfoDto
{
    public int BadgeId { get; set; }
    public string BadgeName { get; set; } = string.Empty;
    public int PointToGet { get; set; }
    public string IconUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEarned { get; set; }
    public DateTime? EarnedAt { get; set; }
}
