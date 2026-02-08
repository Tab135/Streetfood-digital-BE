using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Category;

public class CreateCategoryDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
