using BO.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserSetupController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserSetupController(IUserService userService)
        {
            _userService = userService;
        }

        // GET api/user/userinfo-setup
        [HttpGet("userinfo-setup")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserInfoSetup()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var (userInfoSetup, _) = await _userService.GetUserSetupStatusAsync(userId);
                return Ok(new ApiResponse<object>(200, "User info setup status retrieved", new { UserInfoSetup = userInfoSetup }));
            }
            catch (System.Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        // PUT api/user/userinfo-setup
        [HttpPut("userinfo-setup")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkUserInfoSetup()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                await _userService.MarkUserInfoSetupAsync(userId);
                return Ok(new ApiResponse<object>(200, "User info setup marked as complete", null));
            }
            catch (System.Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        // GET api/user/dietary-setup
        [HttpGet("dietary-setup")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDietarySetup()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var (_, dietarySetup) = await _userService.GetUserSetupStatusAsync(userId);
                return Ok(new ApiResponse<object>(200, "Dietary setup status retrieved", new { DietarySetup = dietarySetup }));
            }
            catch (System.Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        // PUT api/user/dietary-setup
        [HttpPut("dietary-setup")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkDietarySetup()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                await _userService.MarkDietarySetupAsync(userId);
                return Ok(new ApiResponse<object>(200, "Dietary setup marked as complete", null));
            }
            catch (System.Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }
    }
}