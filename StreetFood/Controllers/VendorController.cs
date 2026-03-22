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
        private readonly IVendorDietaryPreferenceService _vendorDietaryPreferenceService;

        public VendorController(
            IVendorService vendorService,
            IWebHostEnvironment webHostEnvironment,
            IVendorDietaryPreferenceService vendorDietaryPreferenceService)
        {
            _vendorService = vendorService ?? throw new ArgumentNullException(nameof(vendorService));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _vendorDietaryPreferenceService = vendorDietaryPreferenceService ?? throw new ArgumentNullException(nameof(vendorDietaryPreferenceService));
        }

        // CRUD Operations

        /// <summary>
        /// Claim a Ghost Pin branch
        /// </summary>
        [HttpPost("claim-branch")]
        [Authorize(Roles = "User,Vendor")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClaimGhostPinBranch(
            [FromForm] int branchId,
            List<Microsoft.AspNetCore.Http.IFormFile>? licenseImages,
            [FromServices] IBranchService branchService,
            [FromServices] Service.PaymentsService.IPaymentService paymentService)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                
                var licenseUrls = new List<string>();

                if (licenseImages != null && licenseImages.Count > 0)
                {
                    var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "uploads", "licenses");
                    if (!System.IO.Directory.Exists(uploadsFolder))
                    {
                        System.IO.Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach(var image in licenseImages)
                    {
                        if (image.Length > 0)
                        {
                            var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
                            var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);
                            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }
                            licenseUrls.Add("http://159.223.47.89:5298" + $"/uploads/licenses/{uniqueFileName}");
                        }
                    }
                }

                var claimResult = await branchService.ClaimUserBranchAsync(branchId, userId, licenseUrls);
                int claimedBranchId = claimResult.BranchId;

                var paymentLink = await paymentService.CreatePaymentLink(userId, claimedBranchId);
                return Ok(new { message = claimResult.Message, paymentLink = paymentLink.PaymentUrl, licenseUrls = licenseUrls });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Create a new vendor account
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "User")]
        [ProducesResponseType(typeof(ApiResponse<VendorResponseDto>), StatusCodes.Status201Created)]
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
                await _vendorDietaryPreferenceService.AssignPreferencesToVendor(vendor.VendorId, createVendorDto.DietaryPreferenceIds);

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
        [ProducesResponseType(typeof(ApiResponse<VendorResponseDto>), StatusCodes.Status200OK)]
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
        [Authorize(Roles = "User,Vendor")]
        [ProducesResponseType(typeof(ApiResponse<VendorResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyVendor()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized( "UNAUTHORIZED");
                }

                var vendor = await _vendorService.GetVendorByUserIdAsync(userId);
                return Ok(vendor);
            }       
            catch (Exception ex)
            {
                return NotFound("VENDOR_NOT_FOUND");
            }
        }

        /// <summary>
        /// Get all vendors (public endpoint)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<VendorResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllVendors([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var vendors = await _vendorService.GetAllVendorsAsync(pageNumber, pageSize);
                return Ok(vendors);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get active vendors only
        /// </summary>
        [HttpGet("active")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<VendorResponseDto>>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Update current user's vendor name
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "User,Vendor")]
        [ProducesResponseType(typeof(ApiResponse<VendorResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMyVendor([FromBody] UpdateVendorDto updateVendorDto)
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

                var vendor = await _vendorService.UpdateVendorAsync(userId, updateVendorDto);
                return Ok(vendor);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "UPDATE_VENDOR_ERROR"));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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

        // Admin Operations

        [HttpPut("{id}/suspend")]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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

        [HttpPut("{id}/reactivate")]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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

        // Dietary Preference Operations

        /// <summary>
        /// Get dietary preferences for a vendor
        /// </summary>
        [HttpGet("{id}/dietary-preferences")]
        [ProducesResponseType(typeof(ApiResponse<List<object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVendorDietaryPreferences(int id)
        {
            try
            {
                var prefs = await _vendorDietaryPreferenceService.GetPreferencesByVendorId(id);
                return Ok(new { message = "Dietary preferences retrieved successfully", data = prefs });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "GET_DIETARY_PREFERENCES_ERROR"));
            }
        }

        /// <summary>
        /// Update dietary preferences for the current user's vendor
        /// </summary>
        [HttpPut("my-vendor/dietary-preferences")]
        [Authorize(Roles = "User,Vendor")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateMyVendorDietaryPreferences([FromBody] List<int> dietaryPreferenceIds)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var vendor = await _vendorService.GetVendorByUserIdAsync(userId);
                var prefs = await _vendorDietaryPreferenceService.AssignPreferencesToVendor(vendor.VendorId, dietaryPreferenceIds);
                return Ok(new { message = "Dietary preferences updated successfully", data = prefs });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, "UPDATE_DIETARY_PREFERENCES_ERROR"));
            }
        }
    }
}
