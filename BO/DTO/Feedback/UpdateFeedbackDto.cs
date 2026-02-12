namespace BO.DTO.Feedback;

public class UpdateFeedbackDto
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
    
    // null = don't change, empty list = remove all, list with values = replace all
    public List<string>? ImageUrls { get; set; }
    
    // null = don't change, empty list = remove all, list with values = replace all
    public List<int>? TagIds { get; set; }
}
