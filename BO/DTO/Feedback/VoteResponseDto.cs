namespace BO.DTO.Feedback;

public class VoteResponseDto
{
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public int NetScore { get; set; }
    public string? UserVote { get; set; } // "up", "down", or null
}
