using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BO.DTO.Auth;
using BO.Entities;
using BO.Exceptions;
using Moq;
using Repository.Interfaces;
using Service;
using Service.Interfaces;
using Service.JWT;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace StreetFood.Tests.Auth
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IOtpVerifyRepository> _otpRepoMock;
        private readonly Mock<ISmsSender> _smsSenderMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IFacebookService> _fbServiceMock;
        private readonly Mock<IGoogleService> _googleServiceMock;
        private readonly Mock<ISettingRepository> _settingRepoMock;
        private readonly Mock<IQuestProgressService> _questServiceMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _jwtServiceMock = new Mock<IJwtService>();
            _otpRepoMock = new Mock<IOtpVerifyRepository>();
            _smsSenderMock = new Mock<ISmsSender>();
            _emailSenderMock = new Mock<IEmailSender>();
            _configMock = new Mock<IConfiguration>();
            _fbServiceMock = new Mock<IFacebookService>();
            _googleServiceMock = new Mock<IGoogleService>();
            _settingRepoMock = new Mock<ISettingRepository>();
            _questServiceMock = new Mock<IQuestProgressService>();

            _userService = new UserService(
                _userRepoMock.Object,
                _jwtServiceMock.Object,
                _otpRepoMock.Object,
                _smsSenderMock.Object,
                _emailSenderMock.Object,
                _configMock.Object,
                _fbServiceMock.Object,
                _googleServiceMock.Object,
                _settingRepoMock.Object,
                _questServiceMock.Object
            );
        }

        // SV_AUTH_01 (UTCID01) - Send OTP Success
        [Fact]
        public async Task SendPhoneLoginOtpAsync_Normal_SendsSms()
        {
            // Arrange
            string phone = "0912345678";
            _otpRepoMock.Setup(r => r.GetRecentOtpsAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<OtpVerify>());
            
            // Act
            var (message, otp) = await _userService.SendPhoneLoginOtpAsync(phone);

            // Assert
            Assert.Contains("gửi đến", message);
            _smsSenderMock.Verify(s => s.SendOtpSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        // SV_AUTH_01 (UTCID02) - Empty Phone
        [Fact]
        public async Task SendPhoneLoginOtpAsync_EmptyPhone_ThrowsException()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _userService.SendPhoneLoginOtpAsync(" "));
            Assert.Equal("Số điện thoại là bắt buộc", ex.Message);
        }

        // SV_AUTH_01 (UTCID03) - Rate Limit
        [Fact]
        public async Task SendPhoneLoginOtpAsync_RateLimitHit_ThrowsException()
        {
            // Arrange
            _otpRepoMock.Setup(r => r.GetRecentOtpsAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(new List<OtpVerify> { new OtpVerify(), new OtpVerify() }); // Limit is 2

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _userService.SendPhoneLoginOtpAsync("0912345678"));
            Assert.Contains("Yêu cầu OTP quá nhiều", ex.Message);
        }

        // SV_AUTH_01 (UTCID04) - Verify Success Existing User
        [Fact]
        public async Task VerifyPhoneOtpAsync_ValidOtp_ExistingUser_ReturnsLoginResponse()
        {
            // Arrange
            string phone = "0912345678";
            string otp = "123456";
            var user = new User { Id = 1, PhoneNumber = phone, PhoneNumberVerified = true, Status = "Active" };
            
            _otpRepoMock.Setup(r => r.GetValidOtpWithDetailAsync(phone, otp))
                .ReturnsAsync((new OtpVerify { Id = 10 }, null));
            _userRepoMock.Setup(r => r.GetByPhoneNumberAsync(phone)).ReturnsAsync(user);
            _jwtServiceMock.Setup(j => j.GenerateToken(user)).Returns("mocked_token");

            // Act
            var result = await _userService.VerifyPhoneOtpAsync(phone, otp);

            // Assert
            Assert.Equal("mocked_token", result.Token);
            Assert.Equal(1, result.User.Id);
        }

        // SV_AUTH_01 (UTCID05) - Invalid OTP
        [Fact]
        public async Task VerifyPhoneOtpAsync_InvalidOtp_ThrowsException()
        {
            // Arrange
            _otpRepoMock.Setup(r => r.GetValidOtpWithDetailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(((OtpVerify?)null, "OTP không hợp lệ"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _userService.VerifyPhoneOtpAsync("0912345678", "999999"));
            Assert.Equal("OTP không hợp lệ", ex.Message);
        }

        // SV_AUTH_01 (UTCID06) - Banned User
        [Fact]
        public async Task VerifyPhoneOtpAsync_BannedUser_ThrowsException()
        {
            // Arrange
            var user = new User { Id = 1, Status = "Banned", PhoneNumberVerified = true };
            _otpRepoMock.Setup(r => r.GetValidOtpWithDetailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((new OtpVerify(), null));
            _userRepoMock.Setup(r => r.GetByPhoneNumberAsync(It.IsAny<string>())).ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _userService.VerifyPhoneOtpAsync("0912345678", "123456"));
            Assert.Contains("Tài khoản của bạn đã bị khóa", ex.Message);
        }

        // SV_AUTH_01 (UTCID07) - New User Auto-Create
        [Fact]
        public async Task VerifyPhoneOtpAsync_ValidOtp_NewUser_CreatesAndReturnsResponse()
        {
            // Arrange
            string phone = "0988887777";
            _otpRepoMock.Setup(r => r.GetValidOtpWithDetailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((new OtpVerify(), null));
            _userRepoMock.Setup(r => r.GetByPhoneNumberAsync(phone)).ReturnsAsync((User?)null);
            _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => { u.Id = 100; return u; });

            // Act
            var result = await _userService.VerifyPhoneOtpAsync(phone, "123456");

            // Assert
            Assert.Equal(100, result.User.Id);
            Assert.True(result.User.PhoneNumberVerified);
            _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
        }
    }
}
