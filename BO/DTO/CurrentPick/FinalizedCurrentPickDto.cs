namespace BO.DTO.CurrentPick;

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
