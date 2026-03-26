using BO.DTO.Quest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestController : ControllerBase
    {
        private readonly IQuestService _questService;
        private readonly IQuestProgressService _questProgressService;

        public QuestController(IQuestService questService, IQuestProgressService questProgressService)
        {
            _questService = questService;
            _questProgressService = questProgressService;
        }

        // ==================== Admin CRUD ====================

        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> CreateQuest([FromBody] CreateQuestDto dto)
        {
            var result = await _questService.CreateQuestAsync(dto);
            return CreatedAtAction(nameof(GetQuestById), new { id = result.QuestId }, new { message = "Quest created successfully", data = result });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> UpdateQuest(int id, [FromBody] UpdateQuestDto dto)
        {
            var result = await _questService.UpdateQuestAsync(id, dto);
            return Ok(new { message = "Quest updated successfully", data = result });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> DeleteQuest(int id)
        {
            var result = await _questService.DeleteQuestAsync(id);
            if (!result)
                return NotFound(new { message = "Quest not found" });
            return Ok(new { message = "Quest deleted successfully" });
        }

        // ==================== Query ====================

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> GetQuests([FromQuery] QuestQueryDto query)
        {
            var result = await _questService.GetQuestsAsync(query);
            return Ok(new { message = "Quests retrieved successfully", data = result });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestById(int id)
        {
            var result = await _questService.GetQuestByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Quest not found" });
            return Ok(new { message = "Quest retrieved successfully", data = result });
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicQuests([FromQuery] QuestQueryDto query)
        {
            var result = await _questService.GetPublicQuestsAsync(query);
            return Ok(new { message = "Public quests retrieved successfully", data = result });
        }

        // ==================== User-facing ====================

        [HttpGet("campaign/{campaignId}/my-progress")]
        [Authorize]
        public async Task<IActionResult> GetCampaignQuestProgress(int campaignId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _questService.GetCampaignQuestProgressAsync(userId, campaignId);
            return Ok(new { message = "Campaign quest progress retrieved successfully", data = result });
        }

        [HttpPost("{questId}/enroll")]
        [Authorize]
        public async Task<IActionResult> EnrollInQuest(int questId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _questService.EnrollInQuestAsync(userId, questId);
            return Ok(new { message = "Enrolled in quest successfully", data = result });
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyQuests([FromQuery] string? status)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _questService.GetMyQuestsAsync(userId, status);
            return Ok(new { message = "User quests retrieved successfully", data = result });
        }

        // ==================== Check-in & Share ====================

        [HttpPost("checkin/{branchId}")]
        [Authorize]
        public async Task<IActionResult> CheckIn(int branchId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _questProgressService.UpdateProgressAsync(userId, "VISIT", 1);
            return Ok(new { message = "Check-in recorded successfully" });
        }

        [HttpPost("share/{branchId}")]
        [Authorize]
        public async Task<IActionResult> ShareStall(int branchId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _questProgressService.UpdateProgressAsync(userId, "SHARE", 1);
            return Ok(new { message = "Share recorded successfully" });
        }
    }
}
