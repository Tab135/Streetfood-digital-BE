using System.ComponentModel.DataAnnotations;

namespace BO.DTO.CurrentPick;

public class JoinCurrentPickRoomDto
{
    [Required]
    [StringLength(12, MinimumLength = 4)]
    public string RoomCode { get; set; } = string.Empty;
}
