using System.ComponentModel.DataAnnotations;

namespace BO.DTO.CurrentPick;

public class CreateCurrentPickRoomDto
{
    [StringLength(255)]
    public string? Title { get; set; }

    public List<int> InitialBranchIds { get; set; } = new();
}
