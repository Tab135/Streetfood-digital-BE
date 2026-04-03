namespace BO.DTO.CurrentPick;

public class CurrentPickMemberDto
{
    public int UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public bool IsHost { get; set; }

    public DateTime JoinedAt { get; set; }
}
