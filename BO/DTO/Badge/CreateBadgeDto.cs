using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Badge;

public class CreateBadgeDto
{
    [Required]
    [StringLength(100)]
    public string BadgeName { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Point must be greater than 0")]
    public int PointToGet { get; set; }

    [Required]
    [StringLength(500)]
    public string IconUrl { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }
}
