using BO.DTO.CurrentPick;

namespace Service.Interfaces;

public interface ICurrentPickPusher
{
    Task PushRoomUpdatedAsync(int roomId, CurrentPickRealtimeEventDto payload);
}
