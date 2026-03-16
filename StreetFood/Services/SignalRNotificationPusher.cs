using BO.DTO.Notification;
using Microsoft.AspNetCore.SignalR;
using Service.Interfaces;
using StreetFood.Hubs;

namespace StreetFood.Services;

public class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushToUserAsync(int userId, NotificationDto notification)
    {
        await _hubContext.Clients
            .Group(userId.ToString())
            .SendAsync("ReceiveNotification", notification);
    }
}
