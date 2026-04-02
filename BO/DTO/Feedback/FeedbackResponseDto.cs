namespace BO.DTO.Feedback;

public class FeedbackResponseDto
{
    public int Id { get; set; }
    public FeedbackUserDto? User { get; set; }
    public int? DishId { get; set; }
    public FeedbackDishDto? Dish { get; set; }
    public int BranchId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? FeedbackXP { get; set; }
    public List<FeedbackImageDto>? Images { get; set; }
    public List<FeedbackTagDto>? Tags { get; set; }
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public int NetScore { get; set; }
    public string? UserVote { get; set; }
    public VendorReplyDto? VendorReply { get; set; }
}

public class FeedbackUserDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }
}

public class FeedbackDishDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
}

public class FeedbackImageDto
{
    public int Id { get; set; }
    public string Url { get; set; }
}

public class FeedbackTagDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}
