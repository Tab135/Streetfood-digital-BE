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
        Task<string> ResendRegistrationOtpAsync(string email, string username);
        Task<string> SendForgetPasswordOtpAsync(string email);
        Task<string> VerifyOtpAsync(string email, string otp);
        Task<string> ResetPasswordAsync(ResetPasswordRequest request);
        Task<string> ResendForgetPasswordOtpAsync(string email);
        Task<User> UpdateUserProfile(int userId, UpdateUserProfileDto updateDto);
    }
}
