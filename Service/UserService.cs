using BO;
using BO.DTO.Auth;
using BO.DTO.Users;
using BO.Entities;
using BO.Exceptions;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Repository.Interfaces;
using Service.Interfaces;
using Service.JWT;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwt_service;
        private readonly IOtpVerifyRepository _otpRepository;
        private readonly ISmsSender _sms_sender;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly IFacebookService _facebookService;
        private readonly IGoogleService _googleService;
        private readonly ISettingRepository _settingRepository;
        private readonly IQuestProgressService _questProgressService;
        private readonly bool _otpDebugMode;

        // Constants
        private const int OtpExpiryMinutes = 3;
        private const int MaxOtpRequestsPerMinute = 2;

        // TEST OTP - Remove this set to disable hardcoded OTP for test accounts
        private static readonly HashSet<string> TestPhoneNumbers = new()
        {
            "0933333333",  // nhatkhoa151204@gmail.com - Vendor
            "0944444444",  // mrnhogiao2011@gmail.com - Manager
            "0900000000",  // phuctan2505@gmail.com - User
            "0911111111",  // nguyenvyscorpio3112004@gmail.com - Admin
            "0922222222"   // vyntyse183836@fpt.edu.vn - Moderator
        };
        private const string TEST_OTP_CODE = "123456";

        public UserService(
            IUserRepository userRepository,
            IJwtService jwtService,
            IOtpVerifyRepository otpVerifyRepository,
            ISmsSender smsSender,
            IEmailSender emailSender,
            IConfiguration configuration,
            IFacebookService facebookService,
            IGoogleService googleService,
            ISettingRepository settingRepository,
            IQuestProgressService questProgressService)
        {
            _userRepository = userRepository;
            _jwt_service = jwtService;
            _otpRepository = otpVerifyRepository;
            _sms_sender = smsSender;
            _emailSender = emailSender;
            _configuration = configuration;
            _facebookService = facebookService;
            _googleService = googleService;
            _settingRepository = settingRepository;
            _questProgressService = questProgressService;
            _otpDebugMode = bool.TryParse(_configuration["Brevo:DebugMode"], out var otpDebugMode) && otpDebugMode;
        }
        public async Task<User> GetUserById(int userId)
        {

            var user = await _userRepository.GetUserById(userId);

            if (user == null)
            {
                throw new DomainExceptions("Không tìm thấy người dùng");
            }

            return user;
        }
        // Helper method to assign runtime next XP limit when returning the User entity directly
        private async Task PopulatedUserNotMappedFieldsAsync(User user)
        {
            if (user == null) return;

            if (user.TierId == 2)
            {
                var goldXpSetting = await _settingRepository.GetByNameAsync("GoldMinXP");
                user.NextTierXP = (goldXpSetting != null && int.TryParse(goldXpSetting.Value, out int gXp)) ? gXp : 3000;
            }
            else if (user.TierId == 3)
            {
                var diamondXpSetting = await _settingRepository.GetByNameAsync("DiamondMinXP");
                user.NextTierXP = (diamondXpSetting != null && int.TryParse(diamondXpSetting.Value, out int dXp)) ? dXp : 10000;
            }
            else
            {
                user.NextTierXP = null; // Diamond max
            }
        }

        public async Task<LoginResponse> GoogleLoginAsync(GoogleAuthDto googleAuthDto)
        {
            try
            {
                var payload = await _googleService.ValidateTokenAndGetPayloadAsync(googleAuthDto);

                var user = await _userRepository.FindOrCreateUserFromGoogleAsync(payload);
                if (user.Status == "Banned")
                {
                    throw new DomainExceptions("Tài khoản của bạn đã bị khóa.");
                }

                if (!string.IsNullOrWhiteSpace(user.Email) && !user.EmailVerified)
                {
                    throw new DomainExceptions("Email chưa được xác minh. Vui lòng xác minh email trước khi đăng nhập bằng email này.");
                }

                var token = _jwt_service.GenerateToken(user);
                await PopulatedUserNotMappedFieldsAsync(user);
                return new LoginResponse
                {
                    Token = token,
                    User = user
                };
            }
            catch (Exception ex)
            {
                throw new DomainExceptions($"Đăng nhập Google thất bại: {ex.Message}");
            }
        }
        // Send OTP for phone-based login 
        public async Task<(string message, string? otp)> SendPhoneLoginOtpAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new DomainExceptions("Số điện thoại là bắt buộc");

            var normalizedPhoneNumber = NormalizePhoneIdentity(phoneNumber);
            var existingUser = await _userRepository.GetByPhoneNumberAsync(normalizedPhoneNumber);
            if (existingUser != null)
            {
                if (existingUser.Status == "Banned")
                {
                    throw new DomainExceptions("Tài khoản của bạn đã bị khóa.");
                }

                if (!existingUser.PhoneNumberVerified)
                {
                    throw new DomainExceptions("Số điện thoại chưa được xác minh. Vui lòng xác minh số điện thoại trong hồ sơ trước khi đăng nhập bằng số này.");
                }
            }

            var otpCode = await SendPhoneOtpAsync(normalizedPhoneNumber);

            if (_otpDebugMode)
            {
                return ($"OTP đã được tạo cho số {normalizedPhoneNumber} ở chế độ debug.", otpCode);
            }

            return ($"OTP đã được gửi đến {normalizedPhoneNumber}. Vui lòng kiểm tra tin nhắn.", null);
        }

        public async Task<LoginResponse> VerifyPhoneOtpAsync(string phoneNumber, string otp)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(otp))
                throw new DomainExceptions("Số điện thoại và OTP là bắt buộc");

            var normalizedPhoneNumber = NormalizePhoneIdentity(phoneNumber);
            var validOtp = await GetValidOtpAsync(normalizedPhoneNumber, otp);

            // Find existing user by phone
            var user = await _userRepository.GetByPhoneNumberAsync(normalizedPhoneNumber);
            if (user == null)
            {
                var newUser = new User
                {
                    UserName = normalizedPhoneNumber,
                    Email = string.Empty,
                    Role = Role.User,
                    CreatedAt = DateTime.UtcNow,
                    EmailVerified = false,
                    PhoneNumber = normalizedPhoneNumber,
                    PhoneNumberVerified = true,
                    FirstName = string.Empty,
                    LastName = string.Empty
                };

                user = await _userRepository.CreateAsync(newUser);
            }
            else if (user.Status == "Banned")
            {
                throw new DomainExceptions("Tài khoản của bạn đã bị khóa.");
            }
            else if (!user.PhoneNumberVerified)
            {
                throw new DomainExceptions("Số điện thoại chưa được xác minh. Vui lòng xác minh số điện thoại trong hồ sơ trước khi đăng nhập bằng số này.");
            }

            var token = _jwt_service.GenerateToken(user);
            await PopulatedUserNotMappedFieldsAsync(user);

            await MarkOtpAsUsedAsync(validOtp.Id, normalizedPhoneNumber, otp);

            return new LoginResponse { Token = token, User = user };
        }

        public async Task<(string message, string? otp)> SendEmailVerificationOtpAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new DomainExceptions("Tài khoản chưa có email để xác minh");
            }

            var normalizedEmail = NormalizeEmailIdentity(user.Email);
            if (user.EmailVerified)
            {
                return ($"Email {normalizedEmail} đã được xác minh trước đó.", null);
            }

            var otpCode = await SendEmailOtpAsync(normalizedEmail);
            if (_otpDebugMode)
            {
                return ($"OTP xác minh email đã được tạo cho {normalizedEmail} ở chế độ debug.", otpCode);
            }

            return ($"OTP xác minh email đã được gửi đến {normalizedEmail}.", null);
        }

        public async Task<(string message, string? otp)> SendPhoneVerificationOtpAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                throw new DomainExceptions("Tài khoản chưa có số điện thoại để xác minh");
            }

            var normalizedPhoneNumber = NormalizePhoneIdentity(user.PhoneNumber);
            if (user.PhoneNumberVerified)
            {
                return ($"Số điện thoại {normalizedPhoneNumber} đã được xác minh trước đó.", null);
            }

            var otpCode = await SendPhoneOtpAsync(normalizedPhoneNumber);

            if (_otpDebugMode)
            {
                return ($"OTP xác minh số điện thoại đã được tạo cho {normalizedPhoneNumber} ở chế độ debug.", otpCode);
            }

            return ($"OTP xác minh số điện thoại đã được gửi đến {normalizedPhoneNumber}.", null);
        }

        public async Task<string> VerifyPendingContactOtpAsync(int userId, string otp)
        {
            if (string.IsNullOrWhiteSpace(otp))
            {
                throw new DomainExceptions("OTP là bắt buộc");
            }

            var user = await GetUserByIdAsync(userId);
            var emailNeedsVerification = !string.IsNullOrWhiteSpace(user.Email) && !user.EmailVerified;
            var phoneNeedsVerification = !string.IsNullOrWhiteSpace(user.PhoneNumber) && !user.PhoneNumberVerified;

            if (!emailNeedsVerification && !phoneNeedsVerification)
            {
                throw new DomainExceptions("Không có thông tin liên hệ nào đang chờ xác minh");
            }

            if (emailNeedsVerification)
            {
                var normalizedEmail = NormalizeEmailIdentity(user.Email!);
                var (validEmailOtp, _) = await _otpRepository.GetValidOtpWithDetailAsync(normalizedEmail, otp);

                if (validEmailOtp != null)
                {
                    user.Email = normalizedEmail;
                    user.EmailVerified = true;
                    await _userRepository.UpdateAsync(user);
                    await MarkOtpAsUsedAsync(validEmailOtp.Id, normalizedEmail, otp);
                    return "email";
                }
            }

            if (phoneNeedsVerification)
            {
                var normalizedPhoneNumber = NormalizePhoneIdentity(user.PhoneNumber!);
                var (validPhoneOtp, _) = await _otpRepository.GetValidOtpWithDetailAsync(normalizedPhoneNumber, otp);

                if (validPhoneOtp != null)
                {
                    user.PhoneNumber = normalizedPhoneNumber;
                    user.PhoneNumberVerified = true;
                    await _userRepository.UpdateAsync(user);
                    await MarkOtpAsUsedAsync(validPhoneOtp.Id, normalizedPhoneNumber, otp);
                    return "phone";
                }
            }

            throw new DomainExceptions("OTP không hợp lệ hoặc đã hết hạn");
        }
        public async Task<User> UpdateUserProfile(int userId, UpdateUserProfileDto updateDto)
        {
            var user = await GetUserByIdAsync(userId);

            if (!string.IsNullOrWhiteSpace(updateDto.Username))
                user.UserName = updateDto.Username.Trim();

            if (!string.IsNullOrWhiteSpace(updateDto.Email))
            {
                var normalizedEmail = NormalizeEmailIdentity(updateDto.Email);
                var currentEmail = string.IsNullOrWhiteSpace(user.Email) ? string.Empty : NormalizeEmailIdentity(user.Email);

                if (!string.Equals(normalizedEmail, currentEmail, StringComparison.Ordinal))
                {
                    await ValidateEmailNotExistsForAnotherUserAsync(normalizedEmail, userId);
                    user.Email = normalizedEmail;
                    user.EmailVerified = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
            {
                var normalizedPhoneNumber = NormalizePhoneIdentity(updateDto.PhoneNumber);
                var currentPhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? string.Empty : NormalizePhoneIdentity(user.PhoneNumber);

                if (!string.Equals(normalizedPhoneNumber, currentPhoneNumber, StringComparison.Ordinal))
                {
                    await ValidatePhoneNotExistsForAnotherUserAsync(normalizedPhoneNumber, userId);
                    user.PhoneNumber = normalizedPhoneNumber;
                    user.PhoneNumberVerified = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(updateDto.AvatarUrl))
                user.AvatarUrl = updateDto.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(updateDto.FirstName))
                user.FirstName = updateDto.FirstName;

            if (!string.IsNullOrWhiteSpace(updateDto.LastName))
                user.LastName = updateDto.LastName;

            await _userRepository.UpdateAsync(user);

            return user;
        }

        // Setup flags
        public async Task<(bool UserInfoSetup, bool DietarySetup)> GetUserSetupStatusAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            return (user.UserInfoSetup, user.DietarySetup);
        }

        public async Task<bool> MarkUserInfoSetupAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user.UserInfoSetup) return true; // already
            user.UserInfoSetup = true;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> MarkDietarySetupAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user.DietarySetup) return true; // already
            user.DietarySetup = true;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<BO.Common.PaginatedResponse<UserProfileDto>> GetUsersAsync(Role? role, int pageNumber, int pageSize)
        {
            var (users, totalCount) = await _userRepository.GetUsersAsync(role, pageNumber, pageSize);
            var mappedUsers = new List<UserProfileDto>();

            // Fetch dynamic admin XP thresholds once for the entire list
            int goldMinXp = 3000;
            int diamondMinXp = 10000;
            var goldXpSetting = await _settingRepository.GetByNameAsync("GoldMinXP");
            if (goldXpSetting != null && int.TryParse(goldXpSetting.Value, out int gXp)) goldMinXp = gXp;
            var diamondXpSetting = await _settingRepository.GetByNameAsync("DiamondMinXP");
            if (diamondXpSetting != null && int.TryParse(diamondXpSetting.Value, out int dXp)) diamondMinXp = dXp;

            foreach (var u in users)
            {
                int? nextTierXp = null;
                if (u.TierId == 2) nextTierXp = goldMinXp;
                else if (u.TierId == 3) nextTierXp = diamondMinXp;

                mappedUsers.Add(new UserProfileDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AvatarUrl = u.AvatarUrl,
                    Status = u.Status,
                    Role = u.Role.ToString(),
                    Point = u.Point,
                    XP = u.XP,
                    TierId = u.TierId,
                    TierName = u.Tier?.Name ?? GetTierNameHardcoded(u.TierId),
                    NextTierXP = nextTierXp
                });
            }

            return new BO.Common.PaginatedResponse<UserProfileDto>(mappedUsers, totalCount, pageNumber, pageSize);
        }

        public async Task<BO.Common.PaginatedResponse<UserProfileDto>> SearchUsersAsync(string keyword, bool onlyUserRole, int pageNumber, int pageSize)
        {
            var (users, totalCount) = await _userRepository.SearchUsersAsync(keyword, onlyUserRole, pageNumber, pageSize);
            var mappedUsers = new List<UserProfileDto>();

            // Fetch dynamic admin XP thresholds once for the entire list
            int goldMinXp = 3000;
            int diamondMinXp = 10000;
            var goldXpSetting = await _settingRepository.GetByNameAsync("GoldMinXP");
            if (goldXpSetting != null && int.TryParse(goldXpSetting.Value, out int gXp)) goldMinXp = gXp;
            var diamondXpSetting = await _settingRepository.GetByNameAsync("DiamondMinXP");
            if (diamondXpSetting != null && int.TryParse(diamondXpSetting.Value, out int dXp)) diamondMinXp = dXp;

            foreach (var u in users)
            {
                int? nextTierXp = null;
                if (u.TierId == 2) nextTierXp = goldMinXp;
                else if (u.TierId == 3) nextTierXp = diamondMinXp;

                mappedUsers.Add(new UserProfileDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AvatarUrl = u.AvatarUrl,
                    Status = u.Status,
                    Role = u.Role.ToString(),
                    Point = u.Point,
                    XP = u.XP,
                    TierId = u.TierId,
                    TierName = u.Tier?.Name ?? GetTierNameHardcoded(u.TierId),
                    NextTierXP = nextTierXp
                });
            }
            return new BO.Common.PaginatedResponse<UserProfileDto>(mappedUsers, totalCount, pageNumber, pageSize);
        }

        public async Task<UserProfileDto> GetUserProfileByIdAsync(int userId)
        {
            var u = await _userRepository.GetUserById(userId);
            if (u == null)
            {
                throw new DomainExceptions("Không tìm thấy người dùng", "ERR_USER_NOT_FOUND");
            }

            int? nextTierXp = null;
            if (u.TierId == 2) // Silver
            {
                var goldXpSetting = await _settingRepository.GetByNameAsync("GoldMinXP");
                if (goldXpSetting != null && int.TryParse(goldXpSetting.Value, out int gXp))
                    nextTierXp = gXp;
                else
                    nextTierXp = 3000; // fallback
            }
            else if (u.TierId == 3) // Gold
            {
                var diamondXpSetting = await _settingRepository.GetByNameAsync("DiamondMinXP");
                if (diamondXpSetting != null && int.TryParse(diamondXpSetting.Value, out int dXp))
                    nextTierXp = dXp;
                else
                    nextTierXp = 10000; // fallback
            }

            return new UserProfileDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                FirstName = u.FirstName,
                LastName = u.LastName,
                AvatarUrl = u.AvatarUrl,
                Status = u.Status,
                Role = u.Role.ToString(),
                Point = u.Point,
                XP = u.XP,
                TierId = u.TierId,
                TierName = u.Tier?.Name ?? GetTierNameHardcoded(u.TierId),
                NextTierXP = nextTierXp
            };
        }

        private string GetTierNameHardcoded(int? tierId)
        {
            return tierId switch
            {
                1 => "Warning",
                2 => "Silver",
                3 => "Gold",
                4 => "Diamond",
                _ => "Unknown"
            };
        }

        // Refactored Facebook login implementation
        public async Task<LoginResponse> FacebookLoginAsync(FacebookAuthDto facebookAuthDto)
        {
            try
            {
                var info = await _facebookService.ValidateTokenAndGetUserAsync(facebookAuthDto.AccessToken);

                // Fallback email when not provided
                var email = info.Email ?? $"fb_{info.Id}@facebook.com";

                var user = await _userRepository.FindOrCreateUserFromFacebookAsync(new FacebookUserInfo
                {
                    Id = info.Id,
                    Email = email,
                    Name = info.Name,
                    FirstName = info.FirstName,
                    LastName = info.LastName,
                    AvatarUrl = info.AvatarUrl
                });

                if (user.Status == "Banned")
                {
                    throw new DomainExceptions("Tài khoản của bạn đã bị khóa.");
                }

                if (!string.IsNullOrWhiteSpace(user.Email) && !user.EmailVerified)
                {
                    throw new DomainExceptions("Email chưa được xác minh. Vui lòng xác minh email trước khi đăng nhập bằng email này.");
                }

                var token = _jwt_service.GenerateToken(user);
                await PopulatedUserNotMappedFieldsAsync(user);
                return new LoginResponse { Token = token, User = user };
            }
            catch (Exception)
            {
                throw new DomainExceptions("Token Facebook không hợp lệ");
            }
        }
        public async Task<bool> PromoteToModeratorAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user.Role == Role.Moderator) return true;
            user.Role = Role.Moderator;
            await _userRepository.UpdateAsync(user);
            return true;
        }
       public async Task<bool> BanUserAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user.Status == "Banned") return false;
            user.Status = "Banned";
            await _userRepository.UpdateAsync(user);
            return true;
        }
        public async Task<bool> UnbanUserAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user.Status != "Banned") return true;
            user.Status = "Active";
            await _userRepository.UpdateAsync(user);
            return true;
        }

        // Helper methods
        private async Task ValidateEmailNotExistsForAnotherUserAsync(string email, int currentUserId)
        {
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null && existingUser.Id != currentUserId)
            {
                throw new DomainExceptions("Email đã tồn tại");
            }
        }

        private async Task ValidatePhoneNotExistsForAnotherUserAsync(string phoneNumber, int currentUserId)
        {
            var existingUser = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
            if (existingUser != null && existingUser.Id != currentUserId)
            {
                throw new DomainExceptions("Số điện thoại đã tồn tại");
            }
        }

        private async Task<User> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null) throw new DomainExceptions("Không tìm thấy người dùng");
            return user;
        }

        private async Task<string> SendPhoneOtpAsync(string normalizedPhoneNumber, string? forcedOtp = null)
        {
            await CheckOtpRequestLimitAsync(normalizedPhoneNumber);

            var otpCode = await GenerateAndStoreOtpAsync(normalizedPhoneNumber, forcedOtp);
            if (forcedOtp == null && !_otpDebugMode)
            {
                var smsPhoneNumber = NormalizePhoneForSms(normalizedPhoneNumber);
                await _sms_sender.SendOtpSmsAsync(smsPhoneNumber, otpCode, OtpExpiryMinutes);
            }

            return otpCode;
        }

        private async Task<string> SendEmailOtpAsync(string normalizedEmail)
        {
            await CheckOtpRequestLimitAsync(normalizedEmail);

            var otpCode = await GenerateAndStoreOtpAsync(normalizedEmail);
            if (!_otpDebugMode)
            {
                var subject = "StreetFood - Ma OTP xac minh email";
                var body = BuildEmailOtpBody(otpCode);
                await _emailSender.SendEmailAsync(normalizedEmail, subject, body);
            }

            return otpCode;
        }

        private string BuildEmailOtpBody(string otpCode)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 520px; margin: 0 auto;'>
                    <h2 style='color: #1f2937;'>Xac minh email StreetFood</h2>
                    <p>Ma OTP cua ban la:</p>
                    <p style='font-size: 28px; letter-spacing: 4px; font-weight: bold; color: #111827;'>{otpCode}</p>
                    <p>Ma co hieu luc trong {OtpExpiryMinutes} phut.</p>
                    <p>Vui long khong chia se ma nay cho bat ky ai.</p>
                </div>";
        }

        private string NormalizeEmailIdentity(string email)
        {
            var normalized = email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new DomainExceptions("Email không hợp lệ");
            }

            return normalized;
        }

        private async Task CheckOtpRequestLimitAsync(string identity)
        {
            var recentOtps = await _otpRepository.GetRecentOtpsAsync(identity, TimeSpan.FromMinutes(1));
            if (recentOtps.Count >= MaxOtpRequestsPerMinute)
                throw new DomainExceptions("Yêu cầu OTP quá nhiều. Vui lòng đợi trước khi thử lại.");
        }

        private async Task<string> GenerateAndStoreOtpAsync(string identity, string? forcedOtp = null)
        {
            // TEST OTP: Use hardcoded OTP for test phone numbers
            var otpCode = forcedOtp ?? (TestPhoneNumbers.Contains(identity) ? TEST_OTP_CODE : GenerateOtp());
            
            var otpVerify = new OtpVerify
            {
                Email = identity,
                Otp = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                IsUsed = false
            };

            await _otpRepository.CreateAsync(otpVerify);
            return otpCode;
        }

        private async Task<BO.Entities.OtpVerify> GetValidOtpAsync(string email, string otp)
        {
            var (validOtp, errorMessage) = await _otpRepository.GetValidOtpWithDetailAsync(email, otp);
            if (validOtp == null)
                throw new DomainExceptions(errorMessage ?? "OTP không hợp lệ hoặc đã hết hạn");
            return validOtp;
        }

        private async Task MarkOtpAsUsedAsync(int otpId, string email, string otp)
        {
            await _otpRepository.MarkOtpAsUsedAsync(otpId);
            await _otpRepository.DeleteUsedOtpAsync(email, otp);
        }

        private string NormalizePhoneIdentity(string phoneNumber)
        {
            var normalized = phoneNumber.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new DomainExceptions("Số điện thoại không hợp lệ");
            }

            return normalized;
        }

        private string NormalizePhoneForSms(string phoneNumber)
        {
            var trimmed = phoneNumber.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new DomainExceptions("Số điện thoại không hợp lệ");

            var hasPlusPrefix = trimmed.StartsWith("+", StringComparison.Ordinal);
            var digitsBuilder = new StringBuilder();

            foreach (var ch in trimmed)
            {
                if (char.IsDigit(ch))
                {
                    digitsBuilder.Append(ch);
                }
            }

            var digits = digitsBuilder.ToString();
            if (digits.Length == 0)
                throw new DomainExceptions("Số điện thoại không hợp lệ");

            if (hasPlusPrefix)
                return "+" + digits;

            if (digits.StartsWith("00", StringComparison.Ordinal))
                return "+" + digits.Substring(2);

            if (digits.StartsWith("0", StringComparison.Ordinal))
                return "+84" + digits.Substring(1);

            if (digits.StartsWith("84", StringComparison.Ordinal))
                return "+" + digits;

            return "+" + digits;
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<bool> AddXPAsync(int userId, int xpAmount)
        {
            if (xpAmount <= 0) return false;

            var user = await _userRepository.GetUserById(userId);
            if (user == null) return false;

            var oldTierId = user.TierId ?? 2;

            user.XP += xpAmount;

            var goldXpSetting = await _settingRepository.GetByNameAsync("GoldMinXP");
            var diamondXpSetting = await _settingRepository.GetByNameAsync("DiamondMinXP");

            var goldXpStr = goldXpSetting?.Value ?? "3000";
            var diamondXpStr = diamondXpSetting?.Value ?? "10000";

            int goldXp = int.TryParse(goldXpStr, out int g) ? g : 3000;
            int diamondXp = int.TryParse(diamondXpStr, out int d) ? d : 10000;

            int newTierId;
            if (user.XP >= diamondXp)
                newTierId = 4; // Diamond
            else if (user.XP >= goldXp)
                newTierId = 3; // Gold
            else
                newTierId = 2; // Silver

            user.TierId = newTierId;
            await _userRepository.UpdateAsync(user);

            if (newTierId > oldTierId)
                await _questProgressService.HandleTierUpAsync(userId, newTierId);

            return true;
        }
    }
}
