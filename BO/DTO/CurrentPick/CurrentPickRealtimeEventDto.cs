namespace BO.DTO.CurrentPick;

public class CurrentPickRealtimeEventDto
{
    public string EventType { get; set; } = string.Empty;

    public CurrentPickRoomResponseDto Room { get; set; } = new();
}
