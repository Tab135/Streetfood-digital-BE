using BO.Common;
using BO.DTO.Users;
using BO.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<UserProfileDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers([FromQuery] Role? role, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _userService.GetUsersAsync(role, pageNumber, pageSize);
                return Ok(new { message = "Users retrieved successfully", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        [Authorize(Roles = "Admin,Vendor,Manager")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<UserProfileDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUsers([FromQuery] string query, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _userService.SearchUsersAsync(query, pageNumber, pageSize);
                return Ok(new { message = "Users retrieved successfully", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserProfileByIdAsync(id);
            return Ok(new { message = "User retrieved successfully", data = user });
        }
        [HttpPost("{id}/promote-moderator")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PromoteToModerator(int id)
        {
            try
            {
                await _userService.PromoteToModeratorAsync(id);
                return Ok(new { Message = "User successfully promoted to Moderator" });
            }
            catch (Exception ex)
            {
                return BadRequest(new  { Message = ex.Message });
            }
        }

        [HttpPost("{id}/ban")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BanUser(int id)
        {
            try
            {
                await _userService.BanUserAsync(id);
                return Ok(new { Message = "User successfully banned" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id}/unban")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UnbanUser(int id)
        {
            try
            {
                await _userService.UnbanUserAsync(id);
                return Ok(new { Message = "User successfully unbanned" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
