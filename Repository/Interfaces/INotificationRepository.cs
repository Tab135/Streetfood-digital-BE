using BO.Entities;

namespace Repository.Interfaces;

public interface INotificationRepository
{
    Task<Notification> Create(Notification notification);
    Task<Notification?> GetById(int notificationId);
    Task<(List<Notification> items, int totalCount)> GetByUserId(int userId, int page, int pageSize);
    Task<int> GetUnreadCount(int userId);
    Task MarkAsRead(int notificationId);
    Task MarkAllAsRead(int userId);
}
