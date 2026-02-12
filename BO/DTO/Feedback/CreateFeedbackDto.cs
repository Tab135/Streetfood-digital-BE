namespace BO.DTO.Feedback;

public class CreateFeedbackDto
{
    public int BranchId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public List<int>? TagIds { get; set; }
}
