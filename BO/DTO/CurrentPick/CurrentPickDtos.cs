using System.ComponentModel.DataAnnotations;

namespace BO.DTO.CurrentPick;

public class CreateCurrentPickRoomDto
{
    [StringLength(255)]
    public string? Title { get; set; }

    public List<int> InitialBranchIds { get; set; } = new();
}

public class JoinCurrentPickRoomDto
{
    [Required]
    [StringLength(12, MinimumLength = 4)]
    public string RoomCode { get; set; } = string.Empty;
}

public class AddCurrentPickBranchDto
{
    [Required]
    public int BranchId { get; set; }
}

public class VoteCurrentPickDto
{
    [Required]
    public int BranchId { get; set; }
}

public class FinalizeCurrentPickDto
{
    public int? BranchId { get; set; }
}

public class CurrentPickMemberDto
{
    public int UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public bool IsHost { get; set; }

    public DateTime JoinedAt { get; set; }
}

public class CurrentPickBranchDto
{
    public int BranchId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AddressDetail { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public double Lat { get; set; }

    public double Long { get; set; }

    public string? ImageUrl { get; set; }

    public int AddedByUserId { get; set; }

    public DateTime AddedAt { get; set; }

    public int VoteCount { get; set; }
}

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

    public string ShareLink { get; set; } = string.Empty;

    public List<CurrentPickMemberDto> Members { get; set; } = new();

    public List<CurrentPickBranchDto> Branches { get; set; } = new();
}

public class CurrentPickShareLinkDto
{
    public int CurrentPickRoomId { get; set; }

    public string RoomCode { get; set; } = string.Empty;

    public string ShareLink { get; set; } = string.Empty;
}

public class FinalizedCurrentPickDto
{
    public int CurrentPickRoomId { get; set; }

    public int FinalizedBranchId { get; set; }

    public string FinalizedBranchName { get; set; } = string.Empty;

    public double Lat { get; set; }

    public double Long { get; set; }

    public string MapUrl { get; set; } = string.Empty;

    public CurrentPickRoomResponseDto Room { get; set; } = new();
}

public class CurrentPickRealtimeEventDto
{
    public string EventType { get; set; } = string.Empty;

    public CurrentPickRoomResponseDto Room { get; set; } = new();
}
