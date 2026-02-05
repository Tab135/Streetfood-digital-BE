using BO.Common;
using BO.DTO.Branch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public BranchController(IBranchService branchService)
        {
            _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
        }

        /// <summary>
        /// Create a new branch for a vendor
        /// </summary>
        [HttpPost("vendor/{vendorId}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CreateBranch(int vendorId, [FromBody] CreateBranchDto createBranchDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var branch = await _branchService.CreateBranchAsync(createBranchDto, vendorId, userId);
                var branchResponse = new BranchResponseDto
                {
                    BranchId = branch.BranchId,
                    VendorId = branch.VendorId,
                    UserId = branch.UserId,
                    Name = branch.Name,
                    PhoneNumber = branch.PhoneNumber,
                    Email = branch.Email,
                    AddressDetail = branch.AddressDetail,
                    BuildingName = branch.BuildingName,
                    Ward = branch.Ward,
                    City = branch.City,
                    Lat = branch.Lat,
                    Long = branch.Long,
                    CreatedAt = branch.CreatedAt,
                    UpdatedAt = branch.UpdatedAt,
                    IsVerified = branch.IsVerified,
                    AvgRating = branch.AvgRating,
                    IsActive = branch.IsActive,
                    IsSubscribed = branch.IsSubscribed
                };

                return CreatedAtAction(nameof(GetBranchById), new { id = branch.BranchId }, branchResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get branch by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBranchById(int id)
        {
            try
            {
                var branch = await _branchService.GetBranchByIdAsync(id);
                return Ok(new ApiResponse<BranchResponseDto>(200, "Branch retrieved successfully", branch));
            }
            catch (Exception ex)
            {
                return NotFound(new ApiResponse<object>(404, ex.Message, null));
            }
        }

        /// <summary>
        /// Get all branches for a vendor
        /// </summary>
        [HttpGet("vendor/{vendorId}")]
        public async Task<IActionResult> GetBranchesByVendorId(int vendorId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetBranchesByVendorIdAsync(vendorId, pageNumber, pageSize);
                return Ok(new ApiResponse<object>(200, "Branches retrieved successfully", branches));
            }
            catch (Exception ex)
            {
                return NotFound(new ApiResponse<object>(404, ex.Message, null));
            }
        }

        /// <summary>
        /// Get all branches (for admin)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllBranches([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetAllBranchesAsync(pageNumber, pageSize);
                return Ok(new ApiResponse<object>(200, "All branches retrieved successfully", branches));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get all active and verified branches (for public map/home page)
        /// Returns only branches that have been verified and are currently active
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveBranches([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetActiveBranchesAsync(pageNumber, pageSize);
                return Ok(new ApiResponse<object>(200, "Active branches retrieved successfully", branches));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Update a branch
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchDto updateBranchDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var branch = await _branchService.UpdateBranchAsync(id, updateBranchDto, userId);
                return Ok(new ApiResponse<BranchResponseDto>(200, "Branch updated successfully", branch));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Delete a branch
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                await _branchService.DeleteBranchAsync(id, userId);
                return Ok(new ApiResponse<object>(200, "Branch deleted successfully", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        // ==================== LICENSE SUBMISSION & VERIFICATION ====================

        /// <summary>
        /// Submit license image for branch verification
        /// </summary>
        [HttpPost("{id}/submit-license")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> SubmitBranchLicense(int id, IFormFile licenseImage)
        {
            try
            {
                if (licenseImage == null || licenseImage.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>(400, "License image is required", null));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                // Save the license image
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "licenses");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{licenseImage.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await licenseImage.CopyToAsync(stream);
                }

                var licenseUrl = $"/uploads/licenses/{uniqueFileName}";

                var result = await _branchService.SubmitBranchLicenseAsync(id, licenseUrl, userId);
                return Ok(new ApiResponse<object>(200, "License submitted successfully. Pending verification.", new
                {
                    BranchId = id,
                    LicenseUrl = licenseUrl,
                    Status = result.Status.ToString()
                }));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get license/registration status for a branch
        /// </summary>
        [HttpGet("{id}/license-status")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetBranchLicenseStatus(int id)
        {
            try
            {
                var result = await _branchService.GetBranchLicenseStatusAsync(id);
                return Ok(new ApiResponse<object>(200, "License status retrieved successfully", new
                {
                    BranchId = id,
                    LicenseUrl = result.LicenseUrl,
                    Status = result.Status.ToString(),
                    RejectReason = result.RejectReason,
                    SubmittedAt = result.CreatedAt,
                    UpdatedAt = result.UpdatedAt
                }));
            }
            catch (Exception ex)
            {
                return NotFound(new ApiResponse<object>(404, ex.Message, null));
            }
        }

        /// <summary>
        /// Get all pending branch registrations (Admin only)
        /// </summary>
        [HttpGet("pending-registrations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingBranchRegistrations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var pendingRegistrations = await _branchService.GetPendingBranchRegistrationsAsync(pageNumber, pageSize);
                return Ok(new ApiResponse<object>(200, "Pending registrations retrieved successfully", pendingRegistrations));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get unverified branches (Admin only)
        /// </summary>
        [HttpGet("unverified")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUnverifiedBranches([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetUnverifiedBranchesAsync(pageNumber, pageSize);
                return Ok(new ApiResponse<object>(200, "Unverified branches retrieved successfully", branches));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Verify a branch (Admin only)
        /// </summary>
        [HttpPut("{id}/verify")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerifyBranch(int id)
        {
            try
            {
                await _branchService.VerifyBranchAsync(id);
                return Ok(new ApiResponse<object>(200, "Branch verified successfully", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Reject a branch registration (Admin only)
        /// </summary>
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectBranchRegistration(int id, [FromBody] RejectBranchDto rejectDto)
        {
            try
            {
                await _branchService.RejectBranchRegistrationAsync(id, rejectDto.Reason);
                return Ok(new ApiResponse<object>(200, "Branch registration rejected", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        // ==================== WORK SCHEDULE OPERATIONS ====================

        /// <summary>
        /// Add a work schedule to a branch
        /// </summary>
        [HttpPost("{branchId}/work-schedules")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AddWorkSchedule(int branchId, [FromBody] AddWorkScheduleDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var schedule = await _branchService.AddWorkScheduleAsync(branchId, dto, userId);
                return Ok(new ApiResponse<object>(200, "Work schedule added successfully", schedule));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get work schedules for a branch
        /// </summary>
        [HttpGet("{branchId}/work-schedules")]
        public async Task<IActionResult> GetBranchWorkSchedules(int branchId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var schedules = await _branchService.GetBranchWorkSchedulesAsync(branchId, pageNumber, pageSize);
                return Ok(new ApiResponse<object>(200, "Work schedules retrieved successfully", schedules));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Update a work schedule
        /// </summary>
        [HttpPut("work-schedules/{scheduleId}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateWorkSchedule(int scheduleId, [FromBody] UpdateWorkScheduleDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var schedule = await _branchService.UpdateWorkScheduleAsync(scheduleId, dto, userId);
                return Ok(new ApiResponse<object>(200, "Work schedule updated successfully", schedule));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Delete a work schedule
        /// </summary>
        [HttpDelete("work-schedules/{scheduleId}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteWorkSchedule(int scheduleId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                await _branchService.DeleteWorkScheduleAsync(scheduleId, userId);
                return Ok(new ApiResponse<object>(200, "Work schedule deleted successfully", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        // ==================== DAY OFF OPERATIONS ====================

        /// <summary>
        /// Add a day off to a branch
        /// </summary>
        [HttpPost("{branchId}/day-offs")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AddDayOff(int branchId, [FromBody] AddDayOffDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var dayOff = await _branchService.AddDayOffAsync(branchId, dto, userId);
                return Ok(new ApiResponse<object>(200, "Day off added successfully", dayOff));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get day offs for a branch
        /// </summary>
        [HttpGet("{branchId}/day-offs")]
        public async Task<IActionResult> GetBranchDayOffs(int branchId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var dayOffs = await _branchService.GetBranchDayOffsAsync(branchId, pageNumber, pageSize);
                return Ok(new ApiResponse<object>(200, "Day offs retrieved successfully", dayOffs));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Delete a day off
        /// </summary>
        [HttpDelete("day-offs/{dayOffId}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteDayOff(int dayOffId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                await _branchService.DeleteDayOffAsync(dayOffId, userId);
                return Ok(new ApiResponse<object>(200, "Day off deleted successfully", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        // ==================== BRANCH IMAGE OPERATIONS ====================

        /// <summary>
        /// Add an image to a branch gallery
        /// </summary>
        [HttpPost("{branchId}/images")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AddBranchImage(int branchId, IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>(400, "Image is required", null));
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                // Save the image
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "branches");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                var imageUrl = $"/uploads/branches/{uniqueFileName}";

                var branchImage = await _branchService.AddBranchImageAsync(branchId, imageUrl, userId);
                return Ok(new ApiResponse<object>(200, "Image added successfully", branchImage));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get images for a branch
        /// </summary>
        [HttpGet("{branchId}/images")]
        public async Task<IActionResult> GetBranchImages(int branchId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var images = await _branchService.GetBranchImagesAsync(branchId, pageNumber, pageSize);
                return Ok(new ApiResponse<object>(200, "Images retrieved successfully", images));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Delete a branch image
        /// </summary>
        [HttpDelete("images/{imageId}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteBranchImage(int imageId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                await _branchService.DeleteBranchImageAsync(imageId, userId);
                return Ok(new ApiResponse<object>(200, "Image deleted successfully", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }
    }
}
