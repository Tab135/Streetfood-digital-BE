using BO.DTO.Notification;
using BO.Entities;
using Moq;
using Repository.Interfaces;
using Service;
using Service.Interfaces;
using Xunit;

namespace StreetFood.Tests.Notification
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _repoMock;
        private readonly Mock<INotificationPusher> _pusherMock;
        private readonly Mock<IExpoPushService> _expoPushMock;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _repoMock = new Mock<INotificationRepository>();
            _pusherMock = new Mock<INotificationPusher>();
            _expoPushMock = new Mock<IExpoPushService>();

            _service = new NotificationService(
                _repoMock.Object,
                _pusherMock.Object,
                _expoPushMock.Object
            );
        }

        [Fact]
        public async Task NotifyAsync_SavesNotification_AndCallsBothPushers()
        {
            // Arrange
            var recipientUserId = 42;
            var type = NotificationType.OrderStatusUpdate;
            var title = "Order Updated";
            var message = "Your order is ready";
            var referenceId = 7;
            var pushData = new { orderId = 7 };

            _repoMock
                .Setup(r => r.Create(It.IsAny<BO.Entities.Notification>()))
                .ReturnsAsync((BO.Entities.Notification n) => { n.NotificationId = 1; return n; });

            _pusherMock
                .Setup(p => p.PushToUserAsync(It.IsAny<int>(), It.IsAny<NotificationDto>()))
                .Returns(Task.CompletedTask);

            _expoPushMock
                .Setup(e => e.SendPushToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyAsync(recipientUserId, type, title, message, referenceId, pushData);

            // Assert: notification was saved with correct data
            _repoMock.Verify(r => r.Create(It.Is<BO.Entities.Notification>(n =>
                n.UserId == recipientUserId &&
                n.Type == type &&
                n.Title == title &&
                n.Message == message &&
                n.ReferenceId == referenceId &&
                n.IsRead == false
            )), Times.Once);

            // Assert: SignalR pusher called for the recipient
            _pusherMock.Verify(p => p.PushToUserAsync(
                recipientUserId,
                It.Is<NotificationDto>(dto =>
                    dto.Title == title &&
                    dto.Message == message
                )
            ), Times.Once);

            // Assert: Expo push called for the recipient
            _expoPushMock.Verify(e => e.SendPushToUserAsync(
                recipientUserId, title, message, pushData
            ), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_WithNullPushData_StillCompletes()
        {
            // Arrange
            _repoMock
                .Setup(r => r.Create(It.IsAny<BO.Entities.Notification>()))
                .ReturnsAsync((BO.Entities.Notification n) => { n.NotificationId = 2; return n; });

            _pusherMock
                .Setup(p => p.PushToUserAsync(It.IsAny<int>(), It.IsAny<NotificationDto>()))
                .Returns(Task.CompletedTask);

            _expoPushMock
                .Setup(e => e.SendPushToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);

            // Act & Assert: should not throw
            await _service.NotifyAsync(1, NotificationType.NewFeedback, "Hi", "Body", null);

            _repoMock.Verify(r => r.Create(It.IsAny<BO.Entities.Notification>()), Times.Once);
            _expoPushMock.Verify(e => e.SendPushToUserAsync(1, "Hi", "Body", null), Times.Once);
        }
    }
}
