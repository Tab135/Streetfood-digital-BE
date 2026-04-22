using BO.DTO.Auth;
using BO.DTO.Users;
using BO.Entities;
using Google.Apis.Auth;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IUserService
    {
        Task<LoginResponse> GoogleLoginAsync(GoogleAuthDto googleAuthDto);
        Task<LoginResponse> FacebookLoginAsync(FacebookAuthDto facebookAuthDto);
        Task<(string message, string? otp)> SendPhoneLoginOtpAsync(string phoneNumber);
        Task<LoginResponse> VerifyPhoneOtpAsync(string phoneNumber, string otp);
        Task<(string message, string? otp)> SendEmailVerificationOtpAsync(int userId);
        Task<(string message, string? otp)> SendPhoneVerificationOtpAsync(int userId);
        Task<string> VerifyPendingContactOtpAsync(int userId, string otp);
        Task<User> UpdateUserProfile(int userId, UpdateUserProfileDto updateDto);
        Task<User> GetUserById(int userId);

        Task<bool> PromoteToModeratorAsync(int userId);
        Task<bool> BanUserAsync(int userId);
        Task<bool> UnbanUserAsync(int userId);

        // Setup flags
        Task<(bool UserInfoSetup, bool DietarySetup)> GetUserSetupStatusAsync(int userId);
        Task<bool> MarkUserInfoSetupAsync(int userId);
        Task<bool> MarkDietarySetupAsync(int userId);

        Task<BO.Common.PaginatedResponse<UserProfileDto>> GetUsersAsync(Role? role, int pageNumber, int pageSize);
        Task<BO.Common.PaginatedResponse<UserProfileDto>> SearchUsersAsync(string keyword, bool onlyUserRole, int pageNumber, int pageSize);
        Task<UserProfileDto> GetUserProfileByIdAsync(int userId);

        Task<bool> AddXPAsync(int userId, int xpAmount);
    }
}
