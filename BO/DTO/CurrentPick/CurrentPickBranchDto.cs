namespace BO.DTO.CurrentPick;

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
