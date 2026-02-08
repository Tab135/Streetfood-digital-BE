using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Dietary;

public class UpdateDietaryPreferenceDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }
}
