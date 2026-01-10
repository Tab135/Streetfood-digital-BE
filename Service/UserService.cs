using BCrypt.Net;
using BO;
using BO.DTO.Auth;
using BO.DTO.Password;
using BO.DTO.Users;
using BO.Entities;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Repository.Interfaces;
using Service.Interfaces;
using Service.JWT;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IOtpVerifyRepository _otpRepository;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        // Constants
        private const int OtpExpiryMinutes = 3;
        private const int MaxOtpRequestsPerMinute = 2;
        private const string PlatformName = "StreetFood Platform";
        private const string PlatformColor = "#1A9288";

        public UserService(
            IUserRepository userRepository,
            IJwtService jwtService,
            IOtpVerifyRepository otpVerifyRepository,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _otpRepository = otpVerifyRepository;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        // Inside UserService.cs

        public async Task<(string token, User user)> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email); //

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
                throw new Exception("Invalid credentials");
            if (!user.EmailVerified)
            {
                throw new Exception("Account is not verified. Please verify your email first.");
            }

            var token = _jwtService.GenerateToken(user);
            return (token, user);
        }

        public async Task<string> SendRegistrationOtpAsync(RegisterDto registerDto)
        {
            // 1. Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email); 

            if (existingUser != null)
            {
                if (existingUser.EmailVerified)
                {
                    throw new Exception("Email already exists");
                }
                else
                {
                    existingUser.UserName = registerDto.Username;
                    existingUser.Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
                    await _userRepository.UpdateAsync(existingUser);
                    existingUser.FirstName = registerDto.FirstName;
                    existingUser.LastName = registerDto.LastName;
                    existingUser.PhoneNumber = registerDto.PhoneNumber;
                }
            }
            else
            {
                var newUser = new User
                {
                    UserName = registerDto.Username,
                    Email = registerDto.Email,
                    Role = Role.User,
                    CreatedAt = DateTime.UtcNow,
                    EmailVerified = false,
                    Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    PhoneNumber = registerDto.PhoneNumber
                };

                await _userRepository.CreateAsync(newUser); 
            }

            var otpCode = await GenerateAndStoreOtpAsync(registerDto.Email); 
            var subject = $"{PlatformName} - Registration Verification Code";
            var body = GenerateOtpEmailTemplate(registerDto.Username, otpCode, "register your account");

            await _emailSender.SendEmailAsync(registerDto.Email, subject, body); 

            return $"OTP sent to {registerDto.Email}. Please check your email and verify within {OtpExpiryMinutes} minutes.";
        }

        public async Task<string> VerifyRegistrationAsync(VerifyRegistrationRequest request)
        {
            var validOtp = await GetValidOtpAsync(request.Email, request.Otp); 

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new Exception("User not found. Please register first.");
            }

            if (user.EmailVerified)
            {
                return "User is already verified. Please log in.";
            }
            user.EmailVerified = true;
            await _userRepository.UpdateAsync(user); 

            await MarkOtpAsUsedAsync(validOtp.Id, request.Email, request.Otp); 

            return "Registration successful. Please log in with your credentials.";
        }

        public async Task<LoginResponse> GoogleLoginAsync(GoogleAuthDto googleAuthDto)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    googleAuthDto.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _configuration["GoogleAuth:ClientId"] }
                    });

                var user = await _userRepository.FindOrCreateUserFromGoogleAsync(payload);
                var token = _jwtService.GenerateToken(user);

                return new LoginResponse
                {
                    Token = token,
                    User = user
                };
            }
            catch (Exception)
            {
                throw new Exception("Invalid Google token");
            }
        }


        public async Task<string> ResendRegistrationOtpAsync(string email, string username)
        {
            await CheckOtpRequestLimitAsync(email); //

            var user = await _userRepository.GetByEmailAsync(email);

            if (user != null)
            {
                if (user.EmailVerified)
                {
                    throw new Exception("Email is already registered and verified. Please login.");
                }
            }
            else
            {
                throw new Exception("No registration request found for this email.");
            }

            var otpCode = await GenerateAndStoreOtpAsync(email);

            var subject = $"{PlatformName} - Registration Verification Code";
            var validUsername = string.IsNullOrEmpty(username) ? user.UserName : username;

            var body = GenerateOtpEmailTemplate(validUsername, otpCode, "register your account");

            await _emailSender.SendEmailAsync(email, subject, body);

            return $"New OTP sent to {email}. Please check your email and verify within {OtpExpiryMinutes} minutes.";
        }

        public async Task<string> SendForgetPasswordOtpAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            await _otpRepository.DeleteAllOtpsByEmailAsync(email);

            var otpCode = await GenerateAndStoreOtpAsync(email);

            var subject = $"{PlatformName} - Reset Your Password";
            var body = GenerateOtpEmailTemplate(user.UserName, otpCode, "reset your password");

            await _emailSender.SendEmailAsync(email, subject, body);

            return $"Password reset OTP sent to {email}. Please check your email and verify within {OtpExpiryMinutes} minutes.";
        }

        public async Task<string> VerifyOtpAsync(string email, string otp)
        {
            await GetValidOtpAsync(email, otp);
            return "OTP is valid";
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var validOtp = await GetValidOtpAsync(request.Email, request.Otp);
            var user = await GetUserByEmailAsync(request.Email);

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);
            await MarkOtpAsUsedAsync(validOtp.Id, request.Email, request.Otp);

            return "Password reset successful. Please log in with your new password.";
        }

        public async Task<string> ResendForgetPasswordOtpAsync(string email)
        {
            await CheckOtpRequestLimitAsync(email);
            var user = await GetUserByEmailAsync(email);

            await _otpRepository.DeleteAllOtpsByEmailAsync(email);
            var otpCode = await GenerateAndStoreOtpAsync(email);

            var subject = $"{PlatformName} - Reset Your Password";
            var body = GenerateOtpEmailTemplate(user.UserName, otpCode, "reset your password");

            await _emailSender.SendEmailAsync(email, subject, body);

            return $"New password reset OTP sent to {email}. Please check your email and verify within {OtpExpiryMinutes} minutes.";
        }

        public async Task<User> UpdateUserProfile(int userId, UpdateUserProfileDto updateDto)
        {
            var user = await GetUserByIdAsync(userId);

            if (!string.IsNullOrWhiteSpace(updateDto.Username))
                user.UserName = updateDto.Username;

            if (!string.IsNullOrWhiteSpace(updateDto.Email) && updateDto.Email != user.Email)
            {
                await ValidateEmailNotExistsAsync(updateDto.Email);
                user.Email = updateDto.Email;
            }

            if (!string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
                user.PhoneNumber = updateDto.PhoneNumber;

            if (!string.IsNullOrWhiteSpace(updateDto.AvatarUrl))
                user.AvatarUrl = updateDto.AvatarUrl;

            await _userRepository.UpdateAsync(user);
            return user;
        }

        // New ChangePassword implementation
        public async Task<string> ChangePassword(string userId, string oldPassword, string newPassword, string confirmNewPassword)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new Exception("Invalid user id");

            if (newPassword != confirmNewPassword)
                throw new Exception("New password and confirmation do not match");

            if (!int.TryParse(userId, out int parsedUserId))
                throw new Exception("Invalid user id format");

            var user = await GetUserByIdAsync(parsedUserId);

            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
                throw new Exception("Old password is incorrect");

            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdatePasswordAsync(parsedUserId, hashed);

            return "Password changed successfully";
        }

        // Helper methods
        private async Task ValidateEmailNotExistsAsync(string email)
        {
            if (await _userRepository.EmailExistsAsync(email))
                throw new Exception("Email already exists");
        }

        private async Task<User> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new Exception("Email not found");
            return user;
        }

        private async Task<User> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
                throw new Exception("User not found");
            return user;
        }

        private async Task CheckOtpRequestLimitAsync(string email)
        {
            var recentOtps = await _otpRepository.GetRecentOtpsAsync(email, TimeSpan.FromMinutes(1));
            if (recentOtps.Count >= MaxOtpRequestsPerMinute)
                throw new Exception("Too many OTP requests. Please wait before trying again.");
        }

        private async Task<string> GenerateAndStoreOtpAsync(string email)
        {
            var otpCode = GenerateOtp();
            var otpVerify = new OtpVerify
            {
                Email = email,
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
                throw new Exception(errorMessage ?? "Invalid or expired OTP");
            return validOtp;
        }

        private async Task MarkOtpAsUsedAsync(int otpId, string email, string otp)
        {
            await _otpRepository.MarkOtpAsUsedAsync(otpId);
            await _otpRepository.DeleteUsedOtpAsync(email, otp);
        }

        private string GenerateOtpEmailTemplate(string username, string otpCode, string action)
        {
            return $@"
<html>
  <body style='margin:0; padding:0; background-color:#eef2f7; font-family:Arial, sans-serif;'>
    <table role='presentation' style='width:100%; border-collapse:collapse; background-color:#eef2f7;'>
      <tr>
        <td align='center' style='padding:40px 0;'>
          <table role='presentation' style='width:100%; max-width:600px; background:#ffffff; border-radius:10px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.08);'>
            <tr>
              <td style='background:{PlatformColor}; padding:20px; text-align:center;'>
                <h2 style='color:#ffffff; margin:0; font-size:22px;'>{PlatformName}</h2>
              </td>
            </tr>
            <tr>
              <td style='padding:35px 40px; text-align:center;'>
                <p style='font-size:16px; color:#333; margin-bottom:18px;'>
                  Hello, <strong>{username}</strong>
                </p>
                <p style='font-size:15px; color:#444; margin-bottom:22px; line-height:1.6;'>
                  We received a request to {action}. Please use the one-time password (OTP) below to continue:
                </p>
                <p style='margin:30px 0;'>
                  <span style='display:inline-block; padding:14px 28px; font-size:26px; font-weight:bold; background:{PlatformColor}; color:#ffffff; border-radius:6px; letter-spacing:3px;'>
                    {otpCode}
                  </span>
                </p>
                <p style='font-size:14px; color:#666; margin-bottom:18px;'>
                  This code will expire in <strong>{OtpExpiryMinutes} minutes</strong>.<br/>
                  Do not share this code with anyone for security reasons.
                </p>
                <p style='font-size:13px; color:#999; line-height:1.5;'>
                  If you did not attempt to {action}, please ignore this email.
                </p>
              </td>
            </tr>
            <tr>
              <td style='background:#f4f6fa; padding:18px; text-align:center;'>
                <p style='font-size:13px; color:#777; margin:0;'>
                  © {DateTime.UtcNow.Year} {PlatformName} — All Rights Reserved
                </p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}
