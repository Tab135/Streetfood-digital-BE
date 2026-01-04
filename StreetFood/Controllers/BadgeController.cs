using BO.DTO.Badge;
using BO.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BadgeController : ControllerBase
    {
        private readonly IBadgeService _badgeService;

        public BadgeController(IBadgeService badgeService)
        {
            _badgeService = badgeService;
        }

        // Admin endpoints
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBadge([FromBody] CreateBadgeDto createBadgeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var badge = await _badgeService.CreateBadge(createBadgeDto);
                return CreatedAtAction(nameof(GetBadgeById), new { id = badge.BadgeId }, badge);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBadge(int id, [FromBody] UpdateBadgeDto updateBadgeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var badge = await _badgeService.UpdateBadge(id, updateBadgeDto);
                return Ok(new
                {
                    message = "Badge updated successfully",
                    badge = badge
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBadge(int id)
        {
            try
            {
                var result = await _badgeService.DeleteBadge(id);
                if (result)
                {
                    return Ok(new { message = "Badge deleted successfully" });
                }
                return NotFound(new { message = "Badge not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Public endpoints
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBadgeById(int id)
        {
            try
            {
                var badge = await _badgeService.GetBadgeById(id);
                if (badge == null)
                {
                    return NotFound(new { message = "Badge not found" });
                }
                return Ok(badge);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBadges()
        {
            try
            {
                var badges = await _badgeService.GetAllBadges();
                return Ok(badges);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // User badge endpoints
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserBadges()
        {
            try
            {
                // Check if the requesting user is authorized to view this user's badges
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;


                var badges = await _badgeService.GetUserBadgesWithInfo(userId);
                return Ok(badges);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("user/check")]
        [Authorize]
        public async Task<IActionResult> CheckAndAwardBadges()
        {
            try
            {
                // Check if the requesting user is authorized
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                await _badgeService.CheckAndAwardBadges(currentUserId);
                return Ok(new { message = "Badges checked and awarded successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("user/{userId}/award/{badgeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AwardBadgeToUser(int userId, int badgeId)
        {
            try
            {
                var userBadge = await _badgeService.AwardBadgeToUser(userId, badgeId);
                return Ok(new
                {
                    message = "Badge awarded successfully",
                    userBadge = userBadge
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}/count")]
        [Authorize]
        public async Task<IActionResult> GetUserBadgeCount(int userId)
        {
            try
            {
                // Check if the requesting user is authorized
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserId != userId && userRole != "Admin")
                {
                    return Forbid();
                }

                var count = await _badgeService.GetUserBadgeCount(userId);
                return Ok(new { badgeCount = count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
