using BO.Common;
using BO.DTO;
using BO.DTO.Badge;
using BO.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using StreetFood.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BadgeController : ControllerBase
    {
        private readonly IBadgeService _badgeService;
        private readonly IS3Service _s3Service;

        public BadgeController(IBadgeService badgeService, IS3Service s3Service)
        {
            _badgeService = badgeService;
            _s3Service = s3Service;
        }

        // Admin endpoints
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<BadgeDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateBadge([FromForm] CreateBadgeDto createBadgeDto, IFormFile imageFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (imageFile == null || imageFile.Length == 0)
                {
                    return BadRequest(new { message = "Badge image is required" });
                }

                var iconUrl = await _s3Service.UploadFileAsync(imageFile, "badges");
                var badge = await _badgeService.CreateBadge(createBadgeDto, iconUrl);
                return CreatedAtAction(nameof(GetBadgeById), new { id = badge.BadgeId }, badge);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<BadgeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateBadge(int id, [FromForm] UpdateBadgeDto updateBadgeDto, IFormFile? imageFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                string? iconUrl = null;
                if (imageFile != null && imageFile.Length > 0)
                {
                    iconUrl = await _s3Service.UploadFileAsync(imageFile, "badges");
                }

                var badge = await _badgeService.UpdateBadge(id, updateBadgeDto, iconUrl);
                return Ok(new
                {
                    message = "Badge updated successfully",
                    data = badge
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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
        [ProducesResponseType(typeof(ApiResponse<BadgeDto>), StatusCodes.Status200OK)]
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
        [ProducesResponseType(typeof(ApiResponse<List<BadgeDto>>), StatusCodes.Status200OK)]
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

        // Admin user badge endpoints
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsersWithBadges([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _badgeService.GetAllUsersWithBadges(pageNumber, pageSize);
                return Ok(result);
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
                    data = userBadge
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("user/{userId}/badge/{badgeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveBadgeFromUser(int userId, int badgeId)
        {
            try
            {
                var result = await _badgeService.RemoveBadgeFromUser(userId, badgeId);
                if (result)
                {
                    return Ok(new { message = "Badge removed from user successfully" });
                }
                return NotFound(new { message = "User badge not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetBadgesByUserId(int userId)
        {
            try
            {
                var badges = await _badgeService.GetUserBadgesWithInfo(userId);
                return Ok(badges);
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
