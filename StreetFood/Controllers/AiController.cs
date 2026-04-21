using BO.Common;
using BO.DTO.AI;
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
    public class AiController : ControllerBase
    {
        private readonly IAiAssistantService _aiAssistantService;

        public AiController(IAiAssistantService aiAssistantService)
        {
            _aiAssistantService = aiAssistantService ?? throw new ArgumentNullException(nameof(aiAssistantService));
        }

        [HttpPost("chat")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(typeof(ApiResponse<AiChatResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _aiAssistantService.ChatAsync(userId, request);
            return Ok(new
            {
                message = "AI response generated successfully",
                data = result
            });
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out userId);
        }
    }
}
