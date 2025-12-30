using BCrypt.Net;
using BO;
using BO.DTO.Auth;
using BO.DTO.Password;
using BO.DTO.Users;
using BO.Entities;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Repository;
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

        public async Task<(string token, User user)> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
                throw new Exception("Invalid credentials");

            var token = _jwtService.GenerateToken(user);
            return (token, user);
        }

        public async Task<string> SendRegistrationOtpAsync(RegisterDto registerDto)
        {

            var emailExists = await _userRepository.EmailExistsAsync(registerDto.Email);
            if (emailExists)
            {
                throw new Exception("Email already exists");
            }


            var otpCode = GenerateOtp();


            await _otpRepository.DeleteAllOtpsByEmailAsync(registerDto.Email);

            // Create new OTP record
            var otpVerify = new OtpVerify
            {
                Email = registerDto.Email,
                Otp = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddMinutes(3), 
                IsUsed = false
            };

            await _otpRepository.CreateAsync(otpVerify);

            var subject = "StreetFood - Registration Verification Code";
            var body = $@"
<html>
  <body style='margin:0; padding:0; background-color:#eef2f7; font-family:Arial, sans-serif;'>
    <table role='presentation' style='width:100%; border-collapse:collapse; background-color:#eef2f7;'>
      <tr>
        <td align='center' style='padding:40px 0;'>
          <table role='presentation' style='width:100%; max-width:600px; background:#ffffff; border-radius:10px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.08);'>
            <tr>
              <td style='background:#1A9288; padding:20px; text-align:center;'>
                <h2 style='color:#ffffff; margin:0; font-size:22px;'>StreetFood Platform</h2>
              </td>
            </tr>
            <tr>
              <td style='padding:35px 40px; text-align:center;'>
                <p style='font-size:16px; color:#333; margin-bottom:18px;'>
                  Hello, <strong>{registerDto.Username}</strong>
                </p>
                <p style='font-size:15px; color:#444; margin-bottom:22px; line-height:1.6;'>
                  We received a request to register your account. Please use the one-time password (OTP) below to continue:
                </p>
                <p style='margin:30px 0;'>
                  <span style='display:inline-block; padding:14px 28px; font-size:26px; font-weight:bold; background:#1A9288; color:#ffffff; border-radius:6px; letter-spacing:3px;'>
                    {otpCode}
                  </span>
                </p>
                <p style='font-size:14px; color:#666; margin-bottom:18px;'>
                  This code will expire in <strong>3 minutes</strong>.<br/>
                  Do not share this code with anyone for security reasons.
                </p>
                <p style='font-size:13px; color:#999; line-height:1.5;'>
                  If you did not attempt to register, please ignore this email.
                </p>
              </td>
            </tr>
            <tr>
              <td style='background:#f4f6fa; padding:18px; text-align:center;'>
                <p style='font-size:13px; color:#777; margin:0;'>
                  © {DateTime.UtcNow.Year} StreetFood Platform — All Rights Reserved
                </p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

            await _emailSender.SendEmailAsync(registerDto.Email, subject, body);

            return $"OTP sent to {registerDto.Email}. Please check your email and verify within 3 minutes.";
        }

        public async Task<string> VerifyRegistrationAsync(VerifyRegistrationRequest request)
        {
            // Get valid OTP
            var (validOtp, errorMessage) = await _otpRepository.GetValidOtpWithDetailAsync(
                request.Email, request.Otp);

            if (validOtp == null)
            {
                throw new Exception(errorMessage ?? "Invalid OTP");
            }

            // Double-check that email doesn't exist (race condition protection)
            var emailExists = await _userRepository.EmailExistsAsync(request.Email);
            if (emailExists)
            {
                throw new Exception("Email already exists");
            }

            // Create the user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Role = Role.User, // Default role
                Createdat = DateTime.UtcNow,
                EmailVerified = true, // Since they verified via OTP
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            await _userRepository.CreateAsync(user);

            // Mark OTP as used
            await _otpRepository.MarkOtpAsUsedAsync(validOtp.Id);
            await _otpRepository.DeleteUsedOtpAsync(request.Email, request.Otp);

            return "Registration successful. Please log in with your credentials.";
        }

        public async Task<LoginResponse> GoogleLoginAsync(GoogleAuthDto googleAuthDto)
        {
            try
            {
                // Verify Google ID token
                var payload = await GoogleJsonWebSignature.ValidateAsync(
                    googleAuthDto.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _configuration["GoogleAuth:ClientId"] }
                    });

                // Find or create user
                var user = await _userRepository.FindOrCreateUserFromGoogleAsync(payload);

                // Generate JWT token
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
            // Prevent too many requests
            var recentOtps = await _otpRepository.GetRecentOtpsAsync(email, TimeSpan.FromMinutes(1));
            if (recentOtps.Count >= 2) // Allow max 2 requests per minute
            {
                throw new Exception("Too many OTP requests. Please wait before trying again.");
            }

            // Check if email already exists (prevent resend for registered users)
            var emailExists = await _userRepository.EmailExistsAsync(email);
            if (emailExists)
            {
                throw new Exception("Email already registered");
            }

            var otpCode = GenerateOtp();

            // Invalidate any existing OTPs for this email
            await _otpRepository.DeleteAllOtpsByEmailAsync(email);

            // Create new OTP record
            var otpVerify = new OtpVerify
            {
                Email = email,
                Otp = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddMinutes(3), // 3 minutes expiry
                IsUsed = false
            };

            await _otpRepository.CreateAsync(otpVerify);

            var subject = "StreetFood - Registration Verification Code";
            var body = $@"
<html>
  <body style='margin:0; padding:0; background-color:#eef2f7; font-family:Arial, sans-serif;'>
    <table role='presentation' style='width:100%; border-collapse:collapse; background-color:#eef2f7;'>
      <tr>
        <td align='center' style='padding:40px 0;'>
          <table role='presentation' style='width:100%; max-width:600px; background:#ffffff; border-radius:10px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.08);'>
            <tr>
              <td style='background:#1A9288; padding:20px; text-align:center;'>
                <h2 style='color:#ffffff; margin:0; font-size:22px;'>StreetFood Platform</h2>
              </td>
            </tr>
            <tr>
              <td style='padding:35px 40px; text-align:center;'>
                <p style='font-size:16px; color:#333; margin-bottom:18px;'>
                  Hello, <strong>{username}</strong>
                </p>
                <p style='font-size:15px; color:#444; margin-bottom:22px; line-height:1.6;'>
                  We received a request to register your account. Please use the one-time password (OTP) below to continue:
                </p>
                <p style='margin:30px 0;'>
                  <span style='display:inline-block; padding:14px 28px; font-size:26px; font-weight:bold; background:#1A9288; color:#ffffff; border-radius:6px; letter-spacing:3px;'>
                    {otpCode}
                  </span>
                </p>
                <p style='font-size:14px; color:#666; margin-bottom:18px;'>
                  This code will expire in <strong>3 minutes</strong>.<br/>
                  Do not share this code with anyone for security reasons.
                </p>
                <p style='font-size:13px; color:#999; line-height:1.5;'>
                  If you did not attempt to register, please ignore this email.
                </p>
              </td>
            </tr>
            <tr>
              <td style='background:#f4f6fa; padding:18px; text-align:center;'>
                <p style='font-size:13px; color:#777; margin:0;'>
                  © {DateTime.UtcNow.Year} StreetFood Platform — All Rights Reserved
                </p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

            await _emailSender.SendEmailAsync(email, subject, body);

            return $"New OTP sent to {email}. Please check your email and verify within 3 minutes.";
        }

        public async Task<string> SendForgetPasswordOtpAsync(string email)
        {
            // Check if email exists
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new Exception("Email not found");
            }

            // Invalidate any existing OTPs for this email
            await _otpRepository.DeleteAllOtpsByEmailAsync(email);

            // Generate 6-digit OTP
            var otpCode = GenerateOtp();

            // Create new OTP record
            var otpVerify = new OtpVerify
            {
                Email = email,
                Otp = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddMinutes(3), // 3 minutes expiry
                IsUsed = false
            };

            await _otpRepository.CreateAsync(otpVerify);

            var subject = "StreetFood - Reset Your Password";
            var body = $@"
<html>
  <body style='margin:0; padding:0; background-color:#eef2f7; font-family:Arial, sans-serif;'>
    <table role='presentation' style='width:100%; border-collapse:collapse; background-color:#eef2f7;'>
      <tr>
        <td align='center' style='padding:40px 0;'>
          <table role='presentation' style='width:100%; max-width:600px; background:#ffffff; border-radius:10px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.08);'>
            <tr>
              <td style='background:#1A9288; padding:20px; text-align:center;'>
                <h2 style='color:#ffffff; margin:0; font-size:22px;'>StreetFood Platform</h2>
              </td>
            </tr>
            <tr>
              <td style='padding:35px 40px; text-align:center;'>
                <p style='font-size:16px; color:#333; margin-bottom:18px;'>
                  Hello, <strong>{user.Username}</strong>
                </p>
                <p style='font-size:15px; color:#444; margin-bottom:22px; line-height:1.6;'>
                  We received a request to reset your password. Please use the one-time password (OTP) below to continue:
                </p>
                <p style='margin:30px 0;'>
                  <span style='display:inline-block; padding:14px 28px; font-size:26px; font-weight:bold; background:#1A9288; color:#ffffff; border-radius:6px; letter-spacing:3px;'>
                    {otpCode}
                  </span>
                </p>
                <p style='font-size:14px; color:#666; margin-bottom:18px;'>
                  This code will expire in <strong>3 minutes</strong>.<br/>
                  Do not share this code with anyone for security reasons.
                </p>
                <p style='font-size:13px; color:#999; line-height:1.5;'>
                  If you did not request a password reset, please ignore this email or contact support if you have concerns.
                </p>
              </td>
            </tr>
            <tr>
              <td style='background:#f4f6fa; padding:18px; text-align:center;'>
                <p style='font-size:13px; color:#777; margin:0;'>
                  © {DateTime.UtcNow.Year} StreetFood Platform — All Rights Reserved
                </p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

            await _emailSender.SendEmailAsync(email, subject, body);

            return $"Password reset OTP sent to {email}. Please check your email and verify within 3 minutes.";
        }

        public async Task<string> VerifyOtpAsync(string email, string otp)
        {
            // Verify OTP without consuming it
            var (validOtp, errorMessage) = await _otpRepository.GetValidOtpWithDetailAsync(email, otp);

            if (validOtp == null)
            {
                throw new Exception(errorMessage ?? "Invalid or expired OTP");
            }

            return "OTP is valid";
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
        {
            // Get valid OTP
            var (validOtp, errorMessage) = await _otpRepository.GetValidOtpWithDetailAsync(
                request.Email, request.Otp);

            if (validOtp == null)
            {
                throw new Exception(errorMessage ?? "Invalid OTP");
            }

            // Get the user
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Update password
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);

            // Mark OTP as used
            await _otpRepository.MarkOtpAsUsedAsync(validOtp.Id);
            await _otpRepository.DeleteUsedOtpAsync(request.Email, request.Otp);

            return "Password reset successful. Please log in with your new password.";
        }

        public async Task<string> ResendForgetPasswordOtpAsync(string email)
        {
            // Prevent too many requests
            var recentOtps = await _otpRepository.GetRecentOtpsAsync(email, TimeSpan.FromMinutes(1));
            if (recentOtps.Count >= 2) // Allow max 2 requests per minute
            {
                throw new Exception("Too many OTP requests. Please wait before trying again.");
            }

            // Check if email exists
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new Exception("Email not found");
            }

            await _otpRepository.DeleteAllOtpsByEmailAsync(email);

            // 
            var otpCode = GenerateOtp();

            // Create new OTP record
            var otpVerify = new OtpVerify
            {
                Email = email,
                Otp = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddMinutes(3), // 3 minutes expiry
                IsUsed = false
            };

            await _otpRepository.CreateAsync(otpVerify);

            var subject = "StreetFood - Reset Your Password";
            var body = $@"
<html>
  <body style='margin:0; padding:0; background-color:#eef2f7; font-family:Arial, sans-serif;'>
    <table role='presentation' style='width:100%; border-collapse:collapse; background-color:#eef2f7;'>
      <tr>
        <td align='center' style='padding:40px 0;'>
          <table role='presentation' style='width:100%; max-width:600px; background:#ffffff; border-radius:10px; overflow:hidden; box-shadow:0 4px 10px rgba(0,0,0,0.08);'>
            <tr>
              <td style='background:#1A9288; padding:20px; text-align:center;'>
                <h2 style='color:#ffffff; margin:0; font-size:22px;'>StreetFood Platform</h2>
              </td>
            </tr>
            <tr>
              <td style='padding:35px 40px; text-align:center;'>
                <p style='font-size:16px; color:#333; margin-bottom:18px;'>
                  Hello, <strong>{user.Username}</strong>
                </p>
                <p style='font-size:15px; color:#444; margin-bottom:22px; line-height:1.6;'>
                  We received a request to reset your password. Please use the one-time password (OTP) below to continue:
                </p>
                <p style='margin:30px 0;'>
                  <span style='display:inline-block; padding:14px 28px; font-size:26px; font-weight:bold; background:#1A9288; color:#ffffff; border-radius:6px; letter-spacing:3px;'>
                    {otpCode}
                  </span>
                </p>
                <p style='font-size:14px; color:#666; margin-bottom:18px;'>
                  This code will expire in <strong>3 minutes</strong>.<br/>
                  Do not share this code with anyone for security reasons.
                </p>
                <p style='font-size:13px; color:#999; line-height:1.5;'>
                  If you did not request a password reset, please ignore this email.
                </p>
              </td>
            </tr>
            <tr>
              <td style='background:#f4f6fa; padding:18px; text-align:center;'>
                <p style='font-size:13px; color:#777; margin:0;'>
                  © {DateTime.UtcNow.Year} StreetFood Platform — All Rights Reserved
                </p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

            await _emailSender.SendEmailAsync(email, subject, body);

            return $"New password reset OTP sent to {email}. Please check your email and verify within 3 minutes.";
        }

        public async Task<User> UpdateUserProfile(int userId, UpdateUserProfileDto updateDto)
        {
            // Get existing user
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Update username if provided
            if (!string.IsNullOrWhiteSpace(updateDto.Username))
            {
                user.Username = updateDto.Username;
            }

            // Update email if provided
            if (!string.IsNullOrWhiteSpace(updateDto.Email))
            {
                // Check if the new email already exists (and it's not the current user's email)
                if (updateDto.Email != user.Email)
                {
                    var emailExists = await _userRepository.EmailExistsAsync(updateDto.Email);
                    if (emailExists)
                    {
                        throw new Exception("Email already exists");
                    }
                    user.Email = updateDto.Email;
                }
            }

            // Update phone number if provided
            if (!string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
            {
                user.Phone_number = updateDto.PhoneNumber;
            }

            // Update avatar URL if provided
            if (!string.IsNullOrWhiteSpace(updateDto.AvatarUrl))
            {
                user.Avatar_url = updateDto.AvatarUrl;
            }

            // Update status if provided
            if (!string.IsNullOrWhiteSpace(updateDto.Status))
            {
                user.Status = updateDto.Status;
            }

            // Save changes
            await _userRepository.UpdateAsync(user);

            return user;
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // 6-digit OTP
        }
    }
}
