using BO.DTO.Users;
using BO.DTO.Dietary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserDietaryController : ControllerBase
    {
        private readonly IUserDietaryPreferenceService _service;

        public UserDietaryController(IUserDietaryPreferenceService service)
        {
            _service = service;
        }

        [HttpPost("user")]
        [Authorize]
        public async Task<IActionResult> AssignPreferences([FromBody] List<int> dietaryPreferenceIds)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (currentUserId == 0) return Unauthorized(new { message = "Invalid user identity" });

                await _service.AssignPreferencesToUser(currentUserId, dietaryPreferenceIds);
                return Ok(new { message = "Preferences updated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetPreferences()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (currentUserId == 0) return Unauthorized(new { message = "Invalid user identity" });

                var prefs = await _service.GetPreferencesByUserId(currentUserId);
                return Ok(prefs);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsersWithPreferences([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _service.GetAllUsersWithPreferences(pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}