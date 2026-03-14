using BO.Common;
using BO.DTO.Notification;
using BO.Entities;

namespace Service.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(int recipientUserId, NotificationType type,
                     string title, string message, int? referenceId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<PaginatedResponse<NotificationDto>> GetUserNotificationsAsync(
                     int userId, int page, int pageSize);
    Task<int> GetUnreadCountAsync(int userId);
}
