using BO.Common;
using BO.DTO.Notification;
using BO.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPusher _pusher;
    private readonly IExpoPushService _expoPushService;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationPusher pusher,
        IExpoPushService expoPushService)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _pusher = pusher ?? throw new ArgumentNullException(nameof(pusher));
        _expoPushService = expoPushService ?? throw new ArgumentNullException(nameof(expoPushService));
    }

    public async Task NotifyAsync(int recipientUserId, NotificationType type,
                                   string title, string message, int? referenceId,
                                   object? pushData = null)
    {
        var notification = new Notification
        {
            UserId = recipientUserId,
            Type = type,
            Title = title,
            Message = message,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _notificationRepository.Create(notification);
        var dto = MapToDto(saved);

        // SignalR push (in-app real-time)
        await _pusher.PushToUserAsync(recipientUserId, dto);

        // Expo push (background/closed app)
        await _expoPushService.SendPushToUserAsync(recipientUserId, title, message, pushData);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _notificationRepository.GetById(notificationId);
        if (notification == null)
            throw new Exception("Notification not found");
        if (notification.UserId != userId)
            throw new Exception("Notification does not belong to this user");

        await _notificationRepository.MarkAsRead(notificationId);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _notificationRepository.MarkAllAsRead(userId);
    }

    public async Task<PaginatedResponse<NotificationDto>> GetUserNotificationsAsync(
        int userId, int page, int pageSize)
    {
        var (notifications, totalCount) = await _notificationRepository.GetByUserId(userId, page, pageSize);
        var items = notifications.Select(MapToDto).ToList();
        return new PaginatedResponse<NotificationDto>(items, totalCount, page, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _notificationRepository.GetUnreadCount(userId);
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        NotificationId = n.NotificationId,
        Type = n.Type.ToString(),
        Title = n.Title,
        Message = n.Message,
        ReferenceId = n.ReferenceId,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
