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
