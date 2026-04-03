using System.ComponentModel.DataAnnotations;

namespace BO.DTO.CurrentPick;

public class VoteCurrentPickDto
{
    [Required]
    public int BranchId { get; set; }
}
