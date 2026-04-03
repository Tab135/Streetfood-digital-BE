namespace BO.DTO.CurrentPick;

public class CurrentPickInviteResponseDto
{
    public int CurrentPickRoomId { get; set; }

    public int InvitedUserId { get; set; }

    public int InvitedByUserId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
