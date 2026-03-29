using BO.DTO.Notification;

namespace Service.Interfaces;

public interface INotificationPusher
{
    Task PushToUserAsync(int userId, NotificationDto notification);
    Task PushPaymentStatusAsync(int userId, long orderCode, string status, int? orderId);
}
