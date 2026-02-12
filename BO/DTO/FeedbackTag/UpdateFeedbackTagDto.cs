using System.ComponentModel.DataAnnotations;

namespace BO.DTO.FeedbackTag;

public class UpdateFeedbackTagDto
{
    [MaxLength(100)]
    public string? TagName { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }
}