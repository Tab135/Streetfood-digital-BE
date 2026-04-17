using BO.Entities;
using Moq;
using Repository.Interfaces;
using Service;
using Xunit;
using Xunit.Abstractions;

namespace StreetFood.Tests.Notification
{
    /// <summary>
    /// Live integration tests — actually POSTs to https://exp.host/--/api/v2/push/send.
    /// Run only when you have network access and a valid Expo push token.
    /// </summary>
    public class ExpoPushIntegrationTests(ITestOutputHelper output)
    {
        private const int TargetUserId = 36;
        private const string TargetToken = "ExponentPushToken[A_gynUJ0betY0byOM3qWPf]";

        private ExpoPushService BuildService()
        {
            var tokenRepoMock = new Mock<IExpoPushTokenRepository>();
            tokenRepoMock
                .Setup(r => r.GetTokensByUserIdAsync(TargetUserId))
                .ReturnsAsync([TargetToken]);

            var httpFactory = new RealHttpClientFactory();
            return new ExpoPushService(tokenRepoMock.Object, httpFactory);
        }

        public static IEnumerable<object[]> CustomerNotificationTypes() =>
        [
            [NotificationType.VendorReply,              "Chủ quán đã phản hồi đánh giá của bạn",       "Cảm ơn bạn đã ghé quán, hẹn gặp lại!"],
            [NotificationType.OrderStatusUpdate,        "Đơn hàng của bạn đã được cập nhật",            "Chủ quán đã xác nhận đơn hàng của bạn."],
            [NotificationType.OrderStatusUpdate,        "Đơn hàng của bạn đã được cập nhật",            "Chủ quán đã từ chối đơn hàng của bạn."],
            [NotificationType.QuestTaskCompleted,       "Bạn đã hoàn thành một nhiệm vụ!",              "Tiếp tục để nhận phần thưởng hấp dẫn nhé."],
            [NotificationType.QuestCompleted,           "Chúc mừng! Bạn đã hoàn thành nhiệm vụ.",       "Phần thưởng đã được ghi vào tài khoản của bạn."],
            [NotificationType.BranchVerificationStatus, "Trạng thái xác minh chi nhánh đã thay đổi",    "Vui lòng kiểm tra trạng thái chi nhánh của bạn."],
        ];

        [Theory]
        [MemberData(nameof(CustomerNotificationTypes))]
        public async Task SendPush_ToUser36_CustomerNotificationTypes(
            NotificationType type, string title, string message)
        {
            var service = BuildService();
            var data = new { type = type.ToString(), referenceId = 1 };

            await service.SendPushToUserAsync(TargetUserId, title, message, data);

            output.WriteLine($"✓ Sent: [{type}] {title}");
        }

        // Helper: real HttpClient without a DI container
        private sealed class RealHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name = "") => new();
        }
    }
}
