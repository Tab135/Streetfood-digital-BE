using BO.DTO.Quest;
using BO.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using StreetFood.Services;
using System.Collections.Generic;
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
        private readonly IS3Service _s3Service;

        public QuestController(IQuestService questService, IQuestProgressService questProgressService, IS3Service s3Service)
        {
            _questService = questService;
            _questProgressService = questProgressService;
            _s3Service = s3Service;
        }

        // ==================== Admin CRUD ====================

        [HttpPost("{id}/image")]
        [Authorize(Roles = "Admin,Moderator")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadQuestImage(int id, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest(new { message = "Image file is required" });

            var imageUrl = await _s3Service.UploadFileAsync(imageFile, "quests");
            var result = await _questService.UpdateQuestImageAsync(id, imageUrl);
            return Ok(new { message = "Quest image uploaded successfully", data = result });
        }

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

        [HttpPut("{id}/tasks")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> ReplaceQuestTasks(int id, [FromBody] List<CreateQuestTaskDto> tasks)
        {
            var result = await _questService.ReplaceQuestTasksAsync(id, tasks);
            return Ok(new { message = "Quest tasks updated successfully", data = result });
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

        [HttpGet("task/{questTaskId}")]
        public async Task<IActionResult> GetQuestTaskById(int questTaskId)
        {
            var result = await _questService.GetQuestTaskByIdAsync(questTaskId);
            if (result == null)
                return NotFound(new { message = "Quest task not found" });
            return Ok(new { message = "Quest task retrieved successfully", data = result });
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicQuests([FromQuery] QuestQueryDto query)
        {
            var result = await _questService.GetPublicQuestsAsync(query);
            return Ok(new { message = "Public quests retrieved successfully", data = result });
        }

        [HttpGet("user-quests")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> GetUserQuests([FromQuery] UserQuestQueryDto query)
        {
            var result = await _questService.GetUserQuestsAsync(query);
            return Ok(new { message = "User quests retrieved successfully", data = result });
        }

        [HttpGet("{questId}/user-quest-tasks")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> GetUserQuestTasksByQuest(int questId, [FromQuery] UserQuestTaskQueryDto query)
        {
            var result = await _questService.GetUserQuestTasksByQuestAsync(questId, query);
            return Ok(new { message = "User quest tasks retrieved successfully", data = result });
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
            try
            {
                var result = await _questService.EnrollInQuestAsync(userId, questId);
                return Ok(new { message = "Enrolled in quest successfully", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{questId}/stop")]
        [Authorize]
        public async Task<IActionResult> StopQuest(int questId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            try
            {
                var result = await _questService.StopQuestAsync(userId, questId);
                return Ok(new { message = "Quest stopped successfully", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyQuests([FromQuery] string? status, [FromQuery] bool? isTierUp, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _questService.GetMyQuestsAsync(userId, status, isTierUp, pageNumber, pageSize);
            return Ok(new { message = "User quests retrieved successfully", data = result });
        }

        // ==================== Progress ====================

        [HttpPost("share/{branchId}")]
        [Authorize]
        public async Task<IActionResult> ShareStall(int branchId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _questProgressService.UpdateProgressAsync(userId, QuestTaskType.SHARE, 1);
            return Ok(new { message = "Share recorded successfully" });
        }
    }
}
