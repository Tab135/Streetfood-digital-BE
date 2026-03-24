using BO.Common;
using BO.DTO.Users;
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
    }
}
