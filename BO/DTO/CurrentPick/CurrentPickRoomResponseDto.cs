namespace BO.DTO.CurrentPick;

public class CurrentPickRoomResponseDto
{
    public int CurrentPickRoomId { get; set; }

    public string RoomCode { get; set; } = string.Empty;

    public string? Title { get; set; }

    public int HostUserId { get; set; }

    public bool IsActive { get; set; }

    public bool IsFinalized { get; set; }

    public int? FinalizedBranchId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public int? MyVotedBranchId { get; set; }

    public List<CurrentPickMemberDto> Members { get; set; } = new();

    public List<CurrentPickBranchDto> Branches { get; set; } = new();
}
