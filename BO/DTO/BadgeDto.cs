using System.ComponentModel.DataAnnotations;

namespace BO.DTO;

public class BadgeDto
{
    public int BadgeId { get; set; }

    public string BadgeName { get; set; } = string.Empty;

    public int PointToGet { get; set; }

    public string IconUrl { get; set; } = string.Empty;

    public string? Description { get; set; }
}