using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Taste;

public class CreateTasteDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
