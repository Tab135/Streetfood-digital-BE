using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Badge;

public class CreateBadgeDto
{
    [Required]
    [StringLength(100)]
    public string BadgeName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }
}
