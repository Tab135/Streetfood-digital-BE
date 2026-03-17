using BO.DTO.Feedback;
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
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IFeedbackVoteService _feedbackVoteService;
        private readonly IVendorReplyService _vendorReplyService;

        public FeedbackController(
            IFeedbackService feedbackService,
            IFeedbackVoteService feedbackVoteService,
            IVendorReplyService vendorReplyService)
        {
            _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
            _feedbackVoteService = feedbackVoteService ?? throw new ArgumentNullException(nameof(feedbackVoteService));
            _vendorReplyService = vendorReplyService ?? throw new ArgumentNullException(nameof(vendorReplyService));
        }

        [HttpPost]
        [Authorize(Roles = "User,Vendor")]
        public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackDto createFeedbackDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var feedback = await _feedbackService.CreateFeedback(createFeedbackDto, userId);
                return CreatedAtAction(nameof(GetFeedbackById), new { id = feedback.Id }, feedback);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{feedbackId}/images")]
        [Authorize(Roles = "User,Vendor")]
        public async Task<IActionResult> UploadFeedbackImages(int feedbackId, List<IFormFile> images)
        {
            try
            {
                if (images == null || images.Count == 0)
                {
                    return BadRequest(new { message = "At least one image is required" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Save the uploaded images
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "feedbacks");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var imageUrls = new List<string>();
                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }
                        imageUrls.Add("http://159.223.47.89:5298" + $"/uploads/feedbacks/{uniqueFileName}");
                    }
                }

                var feedback = await _feedbackService.AddImagesToFeedback(feedbackId, imageUrls, userId);
                return Ok(new
                {
                    message = "Images uploaded successfully",
                    data = feedback
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeedbackById(int id)
        {
            try
            {
                var feedback = await _feedbackService.GetFeedbackById(id);
                return Ok(feedback);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetFeedbackByBranch(
            int branchId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = null)
        {
            try
            {
                int? currentUserId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
                {
                    currentUserId = uid;
                }

                var feedbacks = await _feedbackService.GetFeedbackByBranchId(branchId, pageNumber, pageSize, sortBy, currentUserId);
                return Ok(new
                {
                    message = "Successfully get feedback from branch",
                    data = feedbacks
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetFeedbackByUser(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var feedbacks = await _feedbackService.GetFeedbackByUserId(userId, pageNumber, pageSize);
                return Ok(feedbacks);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("my-feedback")]
        [Authorize(Roles = "User,Vendor")]
        public async Task<IActionResult> GetMyFeedback([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var feedbacks = await _feedbackService.GetFeedbackByUserId(userId, pageNumber, pageSize);
                return Ok(feedbacks);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "User,Vendor")]
        public async Task<IActionResult> UpdateFeedback(int id, [FromBody] UpdateFeedbackDto updateFeedbackDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var feedback = await _feedbackService.UpdateFeedback(id, updateFeedbackDto, userId);
                return Ok(feedback);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "User,Vendor")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var result = await _feedbackService.DeleteFeedback(id, userId);
                if (result)
                {
                    return Ok(new { message = "Feedback deleted successfully" });
                }
                return BadRequest(new { message = "Failed to delete feedback" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("branch/{branchId}/average-rating")]
        public async Task<IActionResult> GetAverageRating(int branchId)
        {
            try
            {
                var avgRating = await _feedbackService.GetAverageRatingByBranch(branchId);
                return Ok(new { branchId, averageRating = avgRating });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("branch/{branchId}/count")]
        public async Task<IActionResult> GetFeedbackCount(int branchId)
        {
            try
            {
                var count = await _feedbackService.GetFeedbackCountByBranch(branchId);
                return Ok(new { branchId, feedbackCount = count });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("branch/{branchId}/rating-range")]
        public async Task<IActionResult> GetFeedbackByRatingRange(
            int branchId,
            [FromQuery] int minRating = 1,
            [FromQuery] int maxRating = 5,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var feedbacks = await _feedbackService.GetFeedbackByRatingRange(branchId, minRating, maxRating, pageNumber, pageSize);
                return Ok(feedbacks);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{feedbackId}/images")]
        public async Task<IActionResult> GetFeedbackImages(int feedbackId)
        {
            try
            {
                var images = await _feedbackService.GetFeedbackImages(feedbackId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{feedbackId}/vote")]
        [Authorize(Roles = "User,Vendor")]
        public async Task<IActionResult> VoteFeedback(int feedbackId, [FromBody] VoteRequestDto voteRequest)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var result = await _feedbackVoteService.Vote(feedbackId, voteRequest.VoteType, userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{feedbackId}/reply")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> CreateReply(int feedbackId, [FromBody] CreateVendorReplyDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var reply = await _vendorReplyService.CreateReply(feedbackId, dto, userId);
                return CreatedAtAction(nameof(GetFeedbackById), new { id = feedbackId }, reply);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{feedbackId}/reply")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> UpdateReply(int feedbackId, [FromBody] UpdateVendorReplyDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var reply = await _vendorReplyService.UpdateReply(feedbackId, dto, userId);
                return Ok(reply);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{feedbackId}/reply")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> DeleteReply(int feedbackId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized(new { message = "User not authenticated" });

                await _vendorReplyService.DeleteReply(feedbackId, userId);
                return Ok(new { message = "Reply deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
