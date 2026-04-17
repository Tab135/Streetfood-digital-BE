namespace BO.DTO.FeedbackTag;

public class FeedbackTagDto
{
    public int TagId { get; set; }
    public string TagName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}