using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BO.DTO.Payments;
using BO.Entities;
using BO.Exceptions;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V1.Payouts;
using Repository.Interfaces;
using Service.Interfaces;
using Service.PaymentsService;
using Xunit;

namespace StreetFood.Tests.Payment
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPaymentRepository> _paymentRepoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IVendorRepository> _vendorRepoMock;
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly Mock<IBranchCampaignRepository> _branchCampaignRepoMock;
        private readonly Mock<ICartRepository> _cartRepoMock;
        private readonly Mock<INotificationPusher> _pusherMock;
        private readonly Mock<INotificationService> _notifServiceMock;
        private readonly Mock<ISettingService> _settingsMock;
        private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger<PaymentService>> _loggerMock;
        private PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _paymentRepoMock = new Mock<IPaymentRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _vendorRepoMock = new Mock<IVendorRepository>();
            _orderRepoMock = new Mock<IOrderRepository>();
            _branchCampaignRepoMock = new Mock<IBranchCampaignRepository>();
            _cartRepoMock = new Mock<ICartRepository>();
            _pusherMock = new Mock<INotificationPusher>();
            _notifServiceMock = new Mock<INotificationService>();
            _settingsMock = new Mock<ISettingService>();
            _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<PaymentService>>();

            // Setup default config for constructor
            _configMock.Setup(c => c["PayOS:DebugMode"]).Returns("true");

            _paymentService = CreateService();
        }

        private PaymentService CreateService(bool debugMode = true)
        {
            _configMock.Setup(c => c["PayOS:DebugMode"]).Returns(debugMode.ToString().ToLower());
            if (!debugMode)
            {
                _configMock.Setup(c => c["PayOS:ClientId"]).Returns("cid");
                _configMock.Setup(c => c["PayOS:ApiKey"]).Returns("key");
                _configMock.Setup(c => c["PayOS:ChecksumKey"]).Returns("checksum");
                _configMock.Setup(c => c["PayOS:PayoutClientId"]).Returns("pcid");
                _configMock.Setup(c => c["PayOS:PayoutApiKey"]).Returns("paky");
                _configMock.Setup(c => c["PayOS:PayoutChecksumKey"]).Returns("pchk");
            }

            return new PaymentService(
                _paymentRepoMock.Object,
                _branchRepoMock.Object,
                _userRepoMock.Object,
                _vendorRepoMock.Object,
                _orderRepoMock.Object,
                _branchCampaignRepoMock.Object,
                _cartRepoMock.Object,
                _pusherMock.Object,
                _notifServiceMock.Object,
                _settingsMock.Object,
                _backgroundJobClientMock.Object,
                _configMock.Object,
                _loggerMock.Object
            );
        }

        // SV_PAY_01 (UTCID01) - Debug Mode Success
        [Fact]
        public async Task RequestUserPayoutAsync_DebugMode_Success_ReturnsResponse()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, MoneyBalance = 500000 };
            var request = new VendorPayoutRequestDto { Amount = 100000, ToAccountNumber = "123", Description = "Test" };
            
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(user);
            
            // Act
            var result = await _paymentService.RequestUserPayoutAsync(userId, request);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("DEBUG", result.PayoutId);
            Assert.Equal(400000, result.CurrentVendorBalance);
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        // SV_PAY_01 (UTCID03) - User Not Found
        [Fact]
        public async Task RequestUserPayoutAsync_UserNotFound_ThrowsException()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetUserById(It.IsAny<int>())).ReturnsAsync((User?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _paymentService.RequestUserPayoutAsync(99, new VendorPayoutRequestDto()));
            Assert.Equal("User not found", ex.Message);
        }

        // SV_PAY_01 (UTCID04) - Amount <= 0
        [Fact]
        public async Task RequestUserPayoutAsync_InvalidAmount_ThrowsException()
        {
            // Arrange
            var user = new User { Id = 1 };
            var request = new VendorPayoutRequestDto { Amount = 0 };
            _userRepoMock.Setup(r => r.GetUserById(1)).ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _paymentService.RequestUserPayoutAsync(1, request));
            Assert.Equal("Amount must be greater than 0", ex.Message);
        }

        // SV_PAY_01 (UTCID05) - Insufficient Balance
        [Fact]
        public async Task RequestUserPayoutAsync_InsufficientBalance_ThrowsException()
        {
            // Arrange
            var user = new User { Id = 1, MoneyBalance = 1000 };
            var request = new VendorPayoutRequestDto { Amount = 5000 };
            _userRepoMock.Setup(r => r.GetUserById(1)).ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _paymentService.RequestUserPayoutAsync(1, request));
            Assert.Equal("Insufficient user balance", ex.Message);
        }
    }
}
