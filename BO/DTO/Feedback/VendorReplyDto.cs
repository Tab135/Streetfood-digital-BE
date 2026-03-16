namespace BO.DTO.Feedback;

public class VendorReplyDto
{
    public int VendorReplyId { get; set; }
    public string Content { get; set; }
    public string RepliedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
