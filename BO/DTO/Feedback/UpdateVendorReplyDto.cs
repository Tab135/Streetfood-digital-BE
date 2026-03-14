using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Feedback;

public class UpdateVendorReplyDto
{
    [Required]
    [StringLength(1000)]
    public string Content { get; set; }
}
