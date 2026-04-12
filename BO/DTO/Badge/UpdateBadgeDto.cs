using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Badge;

public class UpdateBadgeDto
{
    [StringLength(100)]
    public string? BadgeName { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }
}
