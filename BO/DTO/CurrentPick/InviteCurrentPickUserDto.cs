using System.ComponentModel.DataAnnotations;

namespace BO.DTO.CurrentPick;

public class InviteCurrentPickUserDto
{
    [Required]
    public int UserId { get; set; }
}
