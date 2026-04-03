using System.ComponentModel.DataAnnotations;

namespace BO.DTO.CurrentPick;

public class AddCurrentPickBranchDto
{
    [Required]
    public int BranchId { get; set; }
}
