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
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
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
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get branch by ID
        /// If user is a Vendor and owns the branch, returns vendor-specific fields (subscription, license info)
        /// Otherwise returns public data only
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBranchById(int id)
        {
            try
            {
                var branch = await _branchService.GetBranchByIdAsync(id);
                
                // Check if current user is the vendor owner of this branch
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var roleClaim = User.FindFirst(ClaimTypes.Role);
                
                bool isVendorOwner = false;
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) 
                    && roleClaim?.Value == "Vendor" 
                    && branch.UserId == userId)
                {
                    isVendorOwner = true;
                }
                
                // Return appropriate DTO based on ownership
                object responseData = isVendorOwner ? (object)branch : ConvertToPublicDto(branch);
                return Ok(new { message = "Branch retrieved successfully", data = responseData });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Helper method to convert BranchResponseDto to BranchPublicDto
        /// </summary>
        private BO.DTO.Branch.BranchPublicDto ConvertToPublicDto(BO.DTO.Branch.BranchResponseDto full)
        {
            return new BO.DTO.Branch.BranchPublicDto
            {
                BranchId = full.BranchId,
                VendorId = full.VendorId,
                UserId = full.UserId,
                Name = full.Name,
                PhoneNumber = full.PhoneNumber,
                Email = full.Email,
                AddressDetail = full.AddressDetail,
                Ward = full.Ward,
                City = full.City,
                Lat = full.Lat,
                Long = full.Long,
                CreatedAt = full.CreatedAt,
                UpdatedAt = full.UpdatedAt,
                IsVerified = full.IsVerified,
                AvgRating = full.AvgRating,
                IsActive = full.IsActive,

            };
        }


        [HttpGet("vendor/{vendorId}")]
        public async Task<IActionResult> GetBranchesByVendorId(int vendorId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetBranchesByVendorIdAsync(vendorId, pageNumber, pageSize);
                
                // Check if current user is the vendor owner
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var roleClaim = User.FindFirst(ClaimTypes.Role);
                
                bool isVendorOwner = false;
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) 
                    && roleClaim?.Value == "Vendor")
                {
                    // Check if any branch belongs to this user
                    isVendorOwner = branches.Items.Any(b => b.UserId == userId);
                }
                
                // Convert to public DTOs if not vendor owner
                if (!isVendorOwner)
                {
                    var publicBranches = branches.Items.Select(ConvertToPublicDto).ToList();
                    var publicResponse = new BO.Common.PaginatedResponse<BO.DTO.Branch.BranchPublicDto>(
                        publicBranches,
                        branches.TotalCount,
                        branches.CurrentPage,
                        branches.PageSize
                    );
                    return Ok(new { message = "Branches retrieved successfully", data = publicResponse });
                }
                
                return Ok(new { message = "Branches retrieved successfully", data = branches });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
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
                return Ok(new { message = "All branches retrieved successfully", data = branches });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveBranches([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetActiveBranchesAsync(pageNumber, pageSize);
                // Convert all to public DTOs
                var publicBranches = new BO.Common.PaginatedResponse<BO.DTO.Branch.BranchPublicDto>(
                    branches.Items.Select(ConvertToPublicDto).ToList(),
                    branches.TotalCount,
                    branches.CurrentPage,
                    branches.PageSize
                );
                return Ok(new { message = "Active branches retrieved successfully", data = publicBranches });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchDto updateBranchDto)
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

                var branch = await _branchService.UpdateBranchAsync(id, updateBranchDto, userId);
                return Ok(new { message = "Branch updated successfully", data = branch });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _branchService.DeleteBranchAsync(id, userId);
                return Ok(new { message = "Branch deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== LICENSE SUBMISSION & VERIFICATION ====================


        [HttpPost("{id}/submit-license")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> SubmitBranchLicense(int id, List<IFormFile> licenseImages)
        {
            try
            {
                if (licenseImages == null || licenseImages.Count == 0)
                {
                    return BadRequest(new { message = "At least one license image is required" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                // Save the license images
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "licenses");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var licenseUrls = new System.Collections.Generic.List<string>();

                foreach(var image in licenseImages)
                {
                    if (image.Length > 0)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }
                        licenseUrls.Add("http://159.223.47.89:5298" + $"/uploads/licenses/{uniqueFileName}");
                    }
                }

                var result = await _branchService.SubmitBranchLicenseAsync(id, licenseUrls, userId);
                return Ok(new { message = "License submitted successfully. Pending verification.", data = new
                {
                    BranchId = id,
                    LicenseUrls = licenseUrls,
                    Status = result.Status.ToString()
                }});
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/license-status")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetBranchLicenseStatus(int id)
        {
            try
            {
                var result = await _branchService.GetBranchLicenseStatusAsync(id);
                
                var licenseUrls = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(result.LicenseUrl))
                {
                     if (result.LicenseUrl.TrimStart().StartsWith("["))
                     {
                        try 
                        {
                            licenseUrls = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(result.LicenseUrl);
                        }
                        catch
                        {
                            licenseUrls.Add(result.LicenseUrl);
                        }
                     }
                     else
                     {
                        licenseUrls.Add(result.LicenseUrl);
                     }
                }

                return Ok(new { message = "License status retrieved successfully", data = new
                {
                    BranchId = id,
                    LicenseUrls = licenseUrls,
                    Status = result.Status.ToString(),
                    RejectReason = result.RejectReason,
                    SubmittedAt = result.CreatedAt,
                    UpdatedAt = result.UpdatedAt
                }});
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get all pending branch registrations (Moderator only)
        /// </summary>
        [HttpGet("pending-registrations")]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> GetPendingBranchRegistrations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var pendingRegistrations = await _branchService.GetPendingBranchRegistrationsAsync(pageNumber, pageSize);
                return Ok(new { message = "Pending registrations retrieved successfully", data = pendingRegistrations });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get unverified branches (Moderator only)
        /// </summary>
        [HttpGet("unverified")]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> GetUnverifiedBranches([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetUnverifiedBranchesAsync(pageNumber, pageSize);
                return Ok(new { message = "Unverified branches retrieved successfully", data = branches });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Verify a branch (Moderator only)
        /// </summary>
        [HttpPut("{id}/verify")]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> VerifyBranch(int id)
        {
            try
            {
                await _branchService.VerifyBranchAsync(id);
                return Ok(new { message = "Branch verified successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Reject a branch registration (Moderator only)
        /// </summary>
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Moderator")]
        public async Task<IActionResult> RejectBranchRegistration(int id, [FromBody] RejectBranchDto rejectDto)
        {
            try
            {
                await _branchService.RejectBranchRegistrationAsync(id, rejectDto.Reason);
                return Ok(new { message = "Branch registration rejected" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== WORK SCHEDULE OPERATIONS ====================

        /// <summary>
        /// Add a work schedule to a branch
        /// </summary>
        [HttpPost("{branchId}/work-schedules")]
        [Authorize(Roles = "User,Vendor")]
        public async Task<IActionResult> AddWorkSchedule(int branchId, [FromBody] AddWorkScheduleDto dto)
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

                var schedules = await _branchService.AddWorkScheduleAsync(branchId, dto, userId);
                return Ok(new { message = "Work schedules added successfully", data = schedules });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get work schedules for a branch
        /// </summary>
        [HttpGet("{branchId}/work-schedules")]
        public async Task<IActionResult> GetBranchWorkSchedules(int branchId)
        {
            try
            {
                var schedules = await _branchService.GetBranchWorkSchedulesAsync(branchId);
                return Ok(new { message = "Work schedules retrieved successfully", data = schedules });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update a work schedule
        /// </summary>
        [HttpPut("work-schedules/{scheduleId}")]
        [Authorize(Roles = "User,Vendor")]
        public async Task<IActionResult> UpdateWorkSchedule(int scheduleId, [FromBody] UpdateWorkScheduleDto dto)
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

                var schedule = await _branchService.UpdateWorkScheduleAsync(scheduleId, dto, userId);
                return Ok(new { message = "Work schedule updated successfully", data = schedule });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a work schedule
        /// </summary>
        [HttpDelete("work-schedules/{scheduleId}")]
        [Authorize(Roles = "User,Vendor")]
        public async Task<IActionResult> DeleteWorkSchedule(int scheduleId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _branchService.DeleteWorkScheduleAsync(scheduleId, userId);
                return Ok(new { message = "Work schedule deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var dayOff = await _branchService.AddDayOffAsync(branchId, dto, userId);
                return Ok(new { message = "Day off added successfully", data = dayOff });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get day offs for a branch
        /// </summary>
        [HttpGet("{branchId}/day-offs")]
        public async Task<IActionResult> GetBranchDayOffs(int branchId)
        {
            try
            {
                var dayOffs = await _branchService.GetBranchDayOffsAsync(branchId);
                return Ok(new { message = "Day offs retrieved successfully", data = dayOffs });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _branchService.DeleteDayOffAsync(dayOffId, userId);
                return Ok(new { message = "Day off deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== BRANCH IMAGE OPERATIONS ====================
        [HttpPost("{branchId}/images")]
        [Authorize(Roles = "Vendor,User")]
        public async Task<IActionResult> AddBranchImage(int branchId, List<IFormFile> images)
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

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "branches");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var addedImages = new List<object>();
                foreach (var image in images)
                {
                    if (image.Length == 0) continue;

                    var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    var imageUrl = "http://159.223.47.89:5298" + $"/uploads/branches/{uniqueFileName}";
                    var branchImage = await _branchService.AddBranchImageAsync(branchId, imageUrl, userId);
                    addedImages.Add(branchImage);
                }

                return Ok(new { message = "Images added successfully", data = addedImages });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                return Ok(new { message = "Images retrieved successfully", data = images });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a branch image
        /// </summary>
        [HttpDelete("images/{imageId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> DeleteBranchImage(int imageId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _branchService.DeleteBranchImageAsync(imageId, userId);
                return Ok(new { message = "Image deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
