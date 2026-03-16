using BO.DTO.Notification;

namespace Service.Interfaces;

public interface INotificationPusher
{
    Task PushToUserAsync(int userId, NotificationDto notification);
}
