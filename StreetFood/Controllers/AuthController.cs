using BO.DTO.Auth;
using BO.DTO.Password;
using BO.DTO.Users;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using Service.JWT;
using System.Security.Claims;

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

        //[HttpPost("register")]
        //public async Task<IActionResult> SendRegistrationOtp([FromBody] RegisterDto registerDto)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        var message = await _userService.SendRegistrationOtpAsync(registerDto);

        //        return Ok(new
        //        {
        //            message = message,
        //            email = registerDto.Email
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        //[HttpPost("verify-registration")]
        //public async Task<IActionResult> VerifyRegistration([FromBody] VerifyRegistrationRequest request)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(ModelState);
        //        }
        //        var message = await _userService.VerifyRegistrationAsync(request); 

        //        return Ok(new
        //        {
        //            message = message,
        //            redirectTo = "/login"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        //[HttpPost("resend-registration-otp")]
        //public async Task<IActionResult> ResendRegistrationOtp([FromBody] ResendOtpRequest request)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        var message = await _userService.ResendRegistrationOtpAsync(request.Email, request.Username);

        //        return Ok(new
        //        {
        //            message = message,
        //            email = request.Email
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthDto googleAuthDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _userService.GoogleLoginAsync(googleAuthDto);
                return Ok(response);
            }
            catch (InvalidJwtException ex)
            {
                return Unauthorized(new { message = "Invalid Google token" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookAuthDto facebookAuthDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _userService.FacebookLoginAsync(facebookAuthDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        var (token, user) = await _userService.LoginAsync(loginDto);

        //        return Ok(new
        //        {
        //            message = "Login successful",
        //            token = token,
        //            user = new
        //            {
        //                id = user.Id,
        //                username = user.UserName,
        //                email = user.Email,
        //                role = user.Role,
        //                createdAt = user.CreatedAt,
        //                emailVerified = user.EmailVerified,
        //                point = user.Point
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Unauthorized(new { message = ex.Message });
        //    }
        //}

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile() 
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user identity" });
                }
                var user = await _userService.GetUserById(userId);

                return Ok(new
                {
                    userId = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    role = user.Role,
                    phoneNumber = user.PhoneNumber,
                    avatarUrl = user.AvatarUrl,
                    point = user.Point, 
                    createdAt = user.CreatedAt,
                    firstName = user.FirstName,
                    lastName = user.LastName,

                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //[HttpPost("verify-otp")]
        //public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        var message = await _userService.VerifyOtpAsync(request.Email, request.Otp);

        //        return Ok(new
        //        {
        //            message = message,
        //            isValid = true
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message, isValid = false });
        //    }
        //}

        //[HttpPost("forget-password")]
        //public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest request)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        var message = await _userService.SendForgetPasswordOtpAsync(request.Email);

        //        return Ok(new
        //        {
        //            message = message,
        //            email = request.Email
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        //[HttpPost("reset-password")]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        var message = await _userService.ResetPasswordAsync(request);

        //        return Ok(new
        //        {
        //            message = message,
        //            redirectTo = "/login" // Indicate to frontend to redirect to login
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        //[HttpPost("resend-forget-password-otp")]
        //public async Task<IActionResult> ResendForgetPasswordOtp([FromBody] ForgetPasswordRequest request)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        var message = await _userService.ResendForgetPasswordOtpAsync(request.Email);

        //        return Ok(new
        //        {
        //            message = message,
        //            email = request.Email
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            try
            {
                // Get userId from JWT token claims
                var userIdClaim = User.FindFirst("userId")?.Value
                                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var updatedUser = await _userService.UpdateUserProfile(userId, updateDto);

                var newToken = _jwtService.GenerateToken(updatedUser);

                // Return user without password
                return Ok(new
                {
                    message = "Profile updated successfully",
                    token = newToken,
                    user = new
                    {
                        updatedUser.UserName,
                        updatedUser.Email,
                        updatedUser.PhoneNumber,
                        updatedUser.AvatarUrl,
                        updatedUser.FirstName,
                        updatedUser.LastName,   
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //[HttpPost]
        //[Route("change-password")]
        //[Authorize]
        //public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest changePasswordRequest)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //            return BadRequest(ModelState);

        //        // Get current user id from claims
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (string.IsNullOrEmpty(userId))
        //            return Unauthorized(new { message = "Invalid user token" });

        //        var message = await _userService.ChangePassword(userId, changePasswordRequest.OldPassword,
        //            changePasswordRequest.NewPassword, changePasswordRequest.ConfirmNewPassword);

        //        return Ok(new { message = message });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //}
    }
}
