using System.ComponentModel.DataAnnotations;

namespace BO.DTO.Feedback;

public class CreateVendorReplyDto
{
    [Required]
    [StringLength(1000)]
    public string Content { get; set; }
}
