using BO.Common;
using BO.DTO.Taste;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/tastes")]
    [ApiController]
    public class TasteController : ControllerBase
    {
        private readonly ITasteService _tasteService;

        public TasteController(ITasteService tasteService)
        {
            _tasteService = tasteService ?? throw new ArgumentNullException(nameof(tasteService));
        }

        /// <summary>
        /// Create a new taste (Vendor with verified branch only - shared data)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create([FromBody] CreateTasteDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var created = await _tasteService.CreateTasteAsync(createDto, userId);
                return CreatedAtAction(nameof(GetById), new { id = created.TasteId },
                    new ApiResponse<TasteDto>(201, "Taste created successfully", created));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get a taste by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var taste = await _tasteService.GetTasteByIdAsync(id);
                if (taste == null)
                    return NotFound(new ApiResponse<object>(404, "Taste not found", null));

                return Ok(new ApiResponse<TasteDto>(200, "Taste retrieved successfully", taste));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get all tastes (shared data - all vendors can view)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _tasteService.GetAllTastesAsync();
                return Ok(new ApiResponse<List<TasteDto>>(200, "Tastes retrieved successfully", list));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Update a taste (Vendor with verified branch only - shared data, no ownership check)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTasteDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new ApiResponse<object>(400, "Invalid input", ModelState));

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                var updated = await _tasteService.UpdateTasteAsync(id, updateDto, userId);
                return Ok(new ApiResponse<TasteDto>(200, "Taste updated successfully", updated));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Delete a taste (Vendor with verified branch only - shared data, no ownership check)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                await _tasteService.DeleteTasteAsync(id, userId);
                return Ok(new ApiResponse<object>(200, "Taste deleted successfully", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }
    }
}
