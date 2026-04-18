using BO.Common;
using BO.DTO.Auth;
using BO.DTO.Users;
using BO.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using Service.JWT;
using System.Security.Claims;
using static Google.Apis.Requests.BatchRequest;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;

        public AuthController(IUserService userService, IJwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpPost("google-login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthDto googleAuthDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _userService.GoogleLoginAsync(googleAuthDto);
                
                return Ok(new
                {
                    message = "Login successful",
                    token = response.Token,
                    user = new
                    {
                        id = response.User?.Id,
                        username = response.User?.UserName,
                        email = response.User?.Email,
                        emailVerified = response.User?.EmailVerified,
                        role = response.User?.Role,
                        phoneNumber = response.User?.PhoneNumber,
                        phoneNumberVerified = response.User?.PhoneNumberVerified,
                        avatarUrl = response.User?.AvatarUrl,
                        point = response.User?.Point,
                        createdAt = response.User?.CreatedAt,
                        firstName = response.User?.FirstName,
                        lastName = response.User?.LastName,
                        userInfoSetup = response.User?.UserInfoSetup,
                        dietarySetup = response.User?.DietarySetup,
                        status = response.User?.Status,
                        moneyBalance = response.User?.MoneyBalance,
                        tierId = response.User?.TierId,
                        xp = response.User?.XP
                    }
                });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Invalid Google token" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("facebook-login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookAuthDto facebookAuthDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _userService.FacebookLoginAsync(facebookAuthDto);
                
                return Ok(new
                {
                    message = "Login successful",
                    token = response.Token,
                    user = new
                    {
                        id = response.User?.Id,
                        username = response.User?.UserName,
                        email = response.User?.Email,
                        emailVerified = response.User?.EmailVerified,
                        role = response.User?.Role,
                        phoneNumber = response.User?.PhoneNumber,
                        phoneNumberVerified = response.User?.PhoneNumberVerified,
                        avatarUrl = response.User?.AvatarUrl,
                        point = response.User?.Point,
                        createdAt = response.User?.CreatedAt,
                        firstName = response.User?.FirstName,
                        lastName = response.User?.LastName,
                        userInfoSetup = response.User?.UserInfoSetup,
                        dietarySetup = response.User?.DietarySetup,
                        status = response.User?.Status,
                        moneyBalance = response.User?.MoneyBalance,
                        tierId = response.User?.TierId,
                        xp = response.User?.XP
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("phone-login")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PhoneLogin([FromBody] PhoneLoginDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var (message, otp) = await _userService.SendPhoneLoginOtpAsync(request.PhoneNumber);

                return Ok(new
                {
                    message = message,
                    phoneNumber = request.PhoneNumber,
                    otp = otp
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("phone-verify")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PhoneVerify([FromBody] VerifyPhoneOtpDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _userService.VerifyPhoneOtpAsync(request.PhoneNumber, request.Otp);

                return Ok(new
                {
                    message = "Login successful",
                    token = response.Token,
                    user = new
                    {
                        id = response.User?.Id,
                        username = response.User?.UserName,
                        email = response.User?.Email,
                        emailVerified = response.User?.EmailVerified,
                        role = response.User?.Role,
                        phoneNumber = response.User?.PhoneNumber,
                        phoneNumberVerified = response.User?.PhoneNumberVerified,
                        avatarUrl = response.User?.AvatarUrl,
                        point = response.User?.Point,
                        createdAt = response.User?.CreatedAt,
                        firstName = response.User?.FirstName,
                        lastName = response.User?.LastName,
                        userInfoSetup = response.User?.UserInfoSetup,
                        dietarySetup = response.User?.DietarySetup,
                        status = response.User?.Status,
                        moneyBalance = response.User?.MoneyBalance,
                        tierId = response.User?.TierId,
                        xp = response.User?.XP
                    }
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("contact-verification")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartContactVerification()
        {
            try
            {
                if (!TryGetCurrentUserId(out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var user = await _userService.GetUserById(userId);

                var emailNeedsVerification = !string.IsNullOrWhiteSpace(user.Email) && !user.EmailVerified;
                var phoneNeedsVerification = !string.IsNullOrWhiteSpace(user.PhoneNumber) && !user.PhoneNumberVerified;

                if (!emailNeedsVerification && !phoneNeedsVerification)
                {
                    return Ok(new
                    {
                        message = "No pending contact verification",
                        channels = Array.Empty<string>()
                    });
                }

                if (emailNeedsVerification && !phoneNeedsVerification)
                {
                    var (message, otp) = await _userService.SendEmailVerificationOtpAsync(userId);
                    return Ok(new { message, channels = new[] { "email" }, otp });
                }

                if (phoneNeedsVerification && !emailNeedsVerification)
                {
                    var (message, otp) = await _userService.SendPhoneVerificationOtpAsync(userId);
                    return Ok(new { message, channels = new[] { "phone" }, otp });
                }

                var (emailMessage, emailOtp) = await _userService.SendEmailVerificationOtpAsync(userId);
                var (phoneMessage, phoneOtp) = await _userService.SendPhoneVerificationOtpAsync(userId);

                return Ok(new
                {
                    message = "Both email and phone require verification. OTP has been sent for both channels.",
                    channels = new[] { "email", "phone" },
                    email = new { message = emailMessage, otp = emailOtp },
                    phone = new { message = phoneMessage, otp = phoneOtp }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst("userId")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return !string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out userId);
        }
    }
}
