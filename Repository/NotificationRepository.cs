using BO.Entities;
using DAL;
using Repository.Interfaces;

namespace Repository;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDAO _notificationDAO;

    public NotificationRepository(NotificationDAO notificationDAO)
    {
        _notificationDAO = notificationDAO ?? throw new ArgumentNullException(nameof(notificationDAO));
    }

    public async Task<Notification> Create(Notification notification) =>
        await _notificationDAO.CreateAsync(notification);

    public async Task<Notification?> GetById(int notificationId) =>
        await _notificationDAO.GetByIdAsync(notificationId);

    public async Task<(List<Notification> items, int totalCount)> GetByUserId(int userId, int page, int pageSize) =>
        await _notificationDAO.GetByUserIdAsync(userId, page, pageSize);

    public async Task<int> GetUnreadCount(int userId) =>
        await _notificationDAO.GetUnreadCountAsync(userId);

    public async Task MarkAsRead(int notificationId) =>
        await _notificationDAO.MarkAsReadAsync(notificationId);

    public async Task MarkAllAsRead(int userId) =>
        await _notificationDAO.MarkAllAsReadAsync(userId);
}
