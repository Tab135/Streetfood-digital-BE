using BO.Common;
using BO.DTO.Branch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using StreetFood.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;
        private readonly Service.PaymentsService.IPaymentService _paymentService;
        private readonly IS3Service _s3Service;

        public BranchController(IBranchService branchService, Service.PaymentsService.IPaymentService paymentService, IS3Service s3Service)
        {
            _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _s3Service = s3Service ?? throw new ArgumentNullException(nameof(s3Service));
        }

        /// <summary>
        
        // -- GhostPin migrated endpoints --

        [HttpPut("{branchId}/manager")]
        [Authorize(Roles = "Vendor,Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AssignManager(int branchId, [FromBody] AssignManagerDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var success = await _branchService.AssignManagerAsync(branchId, request.ManagerId, userId);
                return Ok(new { message = "Manager assigned successfully", data = success });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("all-ghost-pins")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllApprovedGhostPinBranches([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetAllApprovedGhostPinsAsync(pageNumber, pageSize);
                return Ok(new { message = "All approved ghost pin branches retrieved successfully", data = branches });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-ghost-pin")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyGhostPinBranches([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var branches = await _branchService.GetMyGhostPinBranchesAsync(userId, pageNumber, pageSize);
                return Ok(new { message = "Ghost pin branches retrieved successfully", data = branches });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("manager/my-branch")]
        [Authorize(Roles = "Manager")]
        [ProducesResponseType(typeof(ApiResponse<BranchResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyManagedBranch()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var branch = await _branchService.GetMyManagedBranchAsync(userId);
                return Ok(new { message = "Managed branch retrieved successfully", data = branch });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("user")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<BranchResponseDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateUserBranch([FromBody] CreateUserBranchRequest request)
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

                var branchResponse = await _branchService.CreateUserBranchAsync(request, userId);

                return CreatedAtAction(nameof(GetBranchById), new { id = branchResponse.BranchId }, branchResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Create a new branch for a vendor
        /// </summary>
        [HttpPost("vendor/{vendorId}")]
        [Authorize(Roles = "User,Vendor")]
        [ProducesResponseType(typeof(ApiResponse<BranchResponseDto>), StatusCodes.Status201Created)]
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
                    VendorId = branch.VendorId ?? 0,
                    ManagerId = branch.ManagerId,
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
                    TotalReviewCount = branch.TotalReviewCount,
                    TotalRatingSum = branch.TotalRatingSum,
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
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBranchById(int id)
        {
            try
            {
                var branch = await _branchService.GetBranchByIdAsync(id);
                
                // Check if current user owns this branch's vendor
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                bool isVendorOwner = false;
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    isVendorOwner = await _branchService.IsVendorOwnedByUserAsync(branch.VendorId, userId);
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
                VendorName = full.VendorName,
                ManagerId = full.ManagerId,
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
                IsSubscribed = full.IsSubscribed,
                AvgRating = full.AvgRating,
                IsActive = full.IsActive,

            };
        }


        [HttpGet("vendor/{vendorId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBranchesByVendorId(int vendorId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetBranchesByVendorIdAsync(vendorId, pageNumber, pageSize);
                
                // Check if current user owns this vendor
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                bool isVendorOwner = false;
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    isVendorOwner = await _branchService.IsVendorOwnedByUserAsync(vendorId, userId);
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
        /// Get all branches (for admin and moderator)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<BranchResponseDto>>), StatusCodes.Status200OK)]
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
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ActiveBranchResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveBranches([FromQuery] ActiveBranchFilterDto filter, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get all filtered branches from service
                var allBranches = await _branchService.GetActiveBranchesFilteredAsync(filter);
                
                // Apply pagination at controller level
                var totalCount = allBranches.TotalCount;
                var paginatedItems = allBranches.Items
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var paginatedResponse = new BO.Common.PaginatedResponse<ActiveBranchResponseDto>(
                    paginatedItems,
                    totalCount,
                    pageNumber,
                    pageSize
                );

                return Ok(new { message = "Active branches retrieved successfully", data = paginatedResponse });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{branchId}/similar")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<SimilarBranchResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSimilarBranches(int branchId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var branches = await _branchService.GetSimilarBranchesByDishesAsync(branchId, pageNumber, pageSize);
                return Ok(new { message = "Similar branches retrieved successfully", data = branches });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "User,Vendor,Manager")]
        [ProducesResponseType(typeof(ApiResponse<BranchResponseDto>), StatusCodes.Status200OK)]
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

        [HttpPatch("{id}")]
        [Authorize(Roles = "User,Vendor")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isActive = await _branchService.DeleteBranchAsync(id, userId);
                return Ok(new { message = isActive ? "Mở chi nhánh thành công" : "Đóng chi nhánh thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== LICENSE SUBMISSION & VERIFICATION ====================


        [HttpPost("{id}/submit-license")]
        [Authorize(Roles = "User,Vendor,Manager")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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

                var licenseUrls = new System.Collections.Generic.List<string>();

                foreach(var image in licenseImages)
                {
                    if (image.Length > 0)
                    {
                        var url = await _s3Service.UploadFileAsync(image, "licenses");
                        licenseUrls.Add(url);
                    }
                }

                var result = await _branchService.SubmitBranchLicenseAsync(id, licenseUrls, userId);
                return Ok(new { message = "License submitted successfully. Pending verification.", data = new
                {
                    BranchId = id,
                    RequestedByUserId = result.RequestedByUserId,
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
        [Authorize(Roles = "User,Vendor,Manager")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBranchLicenseStatus(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var result = await _branchService.GetBranchLicenseStatusAsync(id, userId);
                
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
                    RequestedByUserId = result.RequestedByUserId,
                    LicenseUrls = licenseUrls,
                    Status = result.Status.ToString(),
                    VerifiedBy = result.VerifiedBy,
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
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> GetPendingBranchRegistrations([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] int? type = null)
        {
            try
            {
                var pendingRegistrations = await _branchService.GetPendingBranchRegistrationsAsync(pageNumber, pageSize, type);
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
        [Authorize(Roles = "Admin,Moderator")]
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
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> VerifyBranch(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _branchService.VerifyBranchAsync(id, userId);
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
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> RejectBranchRegistration(int id, [FromBody] RejectBranchDto rejectDto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _branchService.RejectBranchRegistrationAsync(id, rejectDto.Reason, userId);
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
        [Authorize(Roles = "User,Vendor,Manager")]
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
        [Authorize(Roles = "User,Vendor,Manager")]
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
        [Authorize(Roles = "User,Vendor,Manager")]
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
        [Authorize(Roles = "User,Vendor,Manager")]
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
        [Authorize(Roles = "User,Vendor,Manager")]
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
        [Authorize(Roles = "Vendor,User,Manager")]
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

                var addedImages = new List<object>();
                foreach (var image in images)
                {
                    if (image.Length == 0) continue;

                    var imageUrl = await _s3Service.UploadFileAsync(image, "branches");
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
        [Authorize(Roles = "Vendor,User,Manager")]
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

