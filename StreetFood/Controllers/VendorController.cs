using BO.Common;
using BO.DTO.Vendor;
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
    public class VendorController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VendorController(
            IVendorService vendorService,
            IWebHostEnvironment webHostEnvironment)
        {
            _vendorService = vendorService ?? throw new ArgumentNullException(nameof(vendorService));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        // CRUD Operations

        /// <summary>
        /// Create a new vendor account
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CreateVendor([FromBody] CreateVendorDto createVendorDto)
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

                var vendor = await _vendorService.CreateVendorAsync(createVendorDto, userId);
                var vendorResponse = await _vendorService.GetVendorByIdAsync(vendor.VendorId);

                return CreatedAtAction(nameof(GetVendorById), new { id = vendor.VendorId }, vendorResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get vendor by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVendorById(int id)
        {
            try
            {
                var vendor = await _vendorService.GetVendorByIdAsync(id);
                return Ok(vendor);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Get my vendor account (current user's vendor)
        /// </summary>
        [HttpGet("my-vendor")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyVendor()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var vendor = await _vendorService.GetVendorByUserIdAsync(userId);
                return Ok(vendor);
            }
            catch (Exception ex)
            {
                return NotFound(new ApiResponse<object>(404, ex.Message, "VENDOR_NOT_FOUND"));
            }
        }

        /// <summary>
        /// Get all vendors (public endpoint)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllVendors([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var vendors = await _vendorService.GetAllVendorsAsync(pageNumber, pageSize);
                return Ok(vendors);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "GET_VENDORS_ERROR"));
            }
        }

        /// <summary>
        /// Get active vendors only
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveVendors([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var vendors = await _vendorService.GetActiveVendorsAsync(pageNumber, pageSize);
                return Ok(vendors);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "GET_VENDORS_ERROR"));
            }
        }

        /*
        /// <summary>
        /// Update vendor information
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] UpdateVendorDto updateVendorDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));
                }

                // Verify user owns this vendor
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var existingVendor = await _vendorService.GetVendorByIdAsync(id);
                if (existingVendor.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var vendor = await _vendorService.UpdateVendorAsync(id, updateVendorDto);
                var vendorResponse = new VendorResponseDto
                {
                    VendorId = vendor.VendorId,
                    UserId = vendor.UserId,
                    Name = vendor.Name,
                    PhoneNumber = vendor.PhoneNumber,
                    Email = vendor.Email,
                    AddressDetail = vendor.AddressDetail,
                    BuildingName = vendor.BuildingName,
                    Ward = vendor.Ward,
                    City = vendor.City,
                    Lat = vendor.Lat,
                    Long = vendor.Long,
                    CreatedAt = vendor.CreatedAt,
                    UpdatedAt = vendor.UpdatedAt,
                    IsVerified = vendor.IsVerified,
                    AvgRating = vendor.AvgRating,
                    IsActive = vendor.IsActive,
                    IsSubscribed = vendor.IsSubscribed
                };

                return Ok(vendorResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "UPDATE_VENDOR_ERROR"));
            }
        }
        */

        /// <summary>
        /// Delete vendor account
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            try
            {
                await _vendorService.DeleteVendorAsync(id);
                return Ok(new { message = "Vendor deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "DELETE_VENDOR_ERROR"));
            }
        }

        // Schedule and Day Off Management (moved to BranchController)

        /*
        /// <summary>
        /// Add work schedule for vendor
        /// </summary>
        [HttpPost("{id}/work-schedules")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AddWorkSchedule(int id, [FromBody] AddWorkScheduleDto scheduleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));
                }

                // Verify user owns this vendor
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var vendor = await _vendorService.GetVendorByIdAsync(id);
                if (vendor.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                await _vendorService.AddWorkScheduleAsync(id, scheduleDto);
                return CreatedAtAction(nameof(GetWorkSchedules), new { id = id },
                    new { message = "Work schedule added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "ADD_SCHEDULE_ERROR"));
            }
        }
        */

        /*
        /// <summary>
        /// Get work schedules for vendor
        /// </summary>
        [HttpGet("{id}/work-schedules")]
        public async Task<IActionResult> GetWorkSchedules(int id)
        {
            try
            {
                var schedules = await _vendorService.GetVendorSchedulesAsync(id);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "GET_SCHEDULES_ERROR"));
            }
        }
        */

        /*
        /// <summary>
        /// Add day off for vendor
        /// </summary>
        [HttpPost("{id}/day-offs")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AddDayOff(int id, [FromBody] AddDayOffDto dayOffDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));
                }

                // Verify user owns this vendor
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var vendor = await _vendorService.GetVendorByIdAsync(id);
                if (vendor.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                await _vendorService.AddDayOffAsync(id, dayOffDto);
                return CreatedAtAction(nameof(GetDayOffs), new { id = id },
                    new { message = "Day off added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "ADD_DAY_OFF_ERROR"));
            }
        }
        */

        /*
        /// <summary>
        /// Get day offs for vendor
        /// </summary>
        [HttpGet("{id}/day-offs")]
        public async Task<IActionResult> GetDayOffs(int id)
        {
            try
            {
                var dayOffs = await _vendorService.GetVendorDayOffsAsync(id);
                return Ok(dayOffs);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "GET_DAY_OFFS_ERROR"));
            }
        }
        */

        // Vendor Images

        /*
        /// <summary>
        /// Add vendor image (gallery)
        /// </summary>
        [HttpPost("{id}/images")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AddVendorImage(int id, IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>(400, "Image is required", "INVALID_FILE"));
                }

                // Verify user owns this vendor
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var vendor = await _vendorService.GetVendorByIdAsync(id);
                if (vendor.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                // Save image to wwroot/uploads
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "vendors");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"vendor_{id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Save relative path to database
                var imageUrl = $"/uploads/vendors/{fileName}";
                await _vendorService.AddVendorImageAsync(id, imageUrl);

                return CreatedAtAction(nameof(GetVendorImages), new { id = id },
                    new { imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "ADD_IMAGE_ERROR"));
            }
        }
        */

        /*
        /// <summary>
        /// Get vendor images
        /// </summary>
        [HttpGet("{id}/images")]
        public async Task<IActionResult> GetVendorImages(int id)
        {
            try
            {
                var images = await _vendorService.GetVendorImagesAsync(id);
                return Ok(images);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "GET_IMAGES_ERROR"));
            }
        }
        */

        // Admin Operations

        /// <summary>
        /// Suspend vendor account (Admin only)
        /// </summary>
        [HttpPut("{id}/suspend")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SuspendVendor(int id)
        {
            try
            {
                await _vendorService.SuspendVendorAsync(id);
                return Ok(new { message = "Vendor suspended successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "SUSPEND_VENDOR_ERROR"));
            }
        }

        /// <summary>
        /// Reactivate vendor account (Admin only)
        /// </summary>
        [HttpPut("{id}/reactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReactivateVendor(int id)
        {
            try
            {
                await _vendorService.ReactivateVendorAsync(id);
                return Ok(new { message = "Vendor reactivated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "REACTIVATE_VENDOR_ERROR"));
            }
        }
    }
}
