using BO.DTO.CurrentPick;
using Microsoft.AspNetCore.SignalR;
using Service.Interfaces;
using StreetFood.Hubs;

namespace StreetFood.Services;

public class SignalRCurrentPickPusher : ICurrentPickPusher
{
    private readonly IHubContext<CurrentPickHub> _hubContext;

    public SignalRCurrentPickPusher(IHubContext<CurrentPickHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushRoomUpdatedAsync(int roomId, CurrentPickRealtimeEventDto payload)
    {
        await _hubContext.Clients
            .Group(CurrentPickHub.GetGroupName(roomId))
            .SendAsync("CurrentPickUpdated", payload);
    }
}
