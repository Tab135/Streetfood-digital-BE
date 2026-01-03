using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Badge;

public class UpdateBadgeDto
{
    [StringLength(100)]
    public string? BadgeName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Point must be greater than 0")]
    public int? PointToGet { get; set; }

    [StringLength(500)]
    public string? IconUrl { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }
}
