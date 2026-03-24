using BO.DTO.Auth;
using BO.DTO.Password;
using BO.DTO.Users;
using BO.Entities;
using Google.Apis.Auth;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IUserService
    {
        Task<(string token, User user)> LoginAsync(LoginDto loginDto);
        Task<string> SendRegistrationOtpAsync(RegisterDto registerDto);
        Task<string> VerifyRegistrationAsync(VerifyRegistrationRequest request);
        Task<LoginResponse> GoogleLoginAsync(GoogleAuthDto googleAuthDto);
        Task<LoginResponse> FacebookLoginAsync(FacebookAuthDto facebookAuthDto);
        Task<string> ResendRegistrationOtpAsync(string email, string username);
        Task<string> SendForgetPasswordOtpAsync(string email);
        Task<string> VerifyOtpAsync(string email, string otp);
        Task<string> ResetPasswordAsync(ResetPasswordRequest request);
        Task<string> ResendForgetPasswordOtpAsync(string email);
        Task<string> SendPhoneLoginOtpAsync(string phoneNumber);
        Task<LoginResponse> VerifyPhoneOtpAsync(string phoneNumber, string otp);
        Task<User> UpdateUserProfile(int userId, UpdateUserProfileDto updateDto);
        Task<string> ChangePassword(string userId, string oldPassword, string newPassword, string confirmNewPassword);
        Task<User> GetUserById(int userId);

        // Setup flags
        Task<(bool UserInfoSetup, bool DietarySetup)> GetUserSetupStatusAsync(int userId);
        Task<bool> MarkUserInfoSetupAsync(int userId);
        Task<bool> MarkDietarySetupAsync(int userId);

        Task<BO.Common.PaginatedResponse<UserProfileDto>> SearchUsersAsync(string keyword, int pageNumber, int pageSize);
        Task<UserProfileDto> GetUserProfileByIdAsync(int userId);
    }
}
