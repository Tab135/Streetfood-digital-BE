using BO.Common;
using BO.DTO.Dish;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/dishes")]
    [ApiController]
    public class DishController : ControllerBase
    {
        private readonly IDishService _dishService;

        public DishController(IDishService dishService)
        {
            _dishService = dishService ?? throw new ArgumentNullException(nameof(dishService));
        }

        /// <summary>
        /// Create a new dish (Vendor only - must own the branch)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CreateDish([FromBody] CreateDishRequest request)
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

                var result = await _dishService.CreateDishAsync(request, userId);
                return CreatedAtAction(nameof(GetDishById), new { id = result.DishId },
                    new ApiResponse<DishResponse>(201, "Dish created successfully", result));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Get a dish by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDishById(int id)
        {
            try
            {
                var result = await _dishService.GetDishByIdAsync(id);
                return Ok(new ApiResponse<DishResponse>(200, "Dish retrieved successfully", result));
            }
            catch (Exception ex)
            {
                return NotFound(new ApiResponse<object>(404, ex.Message, null));
            }
        }

        /// <summary>
        /// Get dish list with optional filtering by BranchId, CategoryId, and Keyword
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDishes(
            [FromQuery] int? branchId,
            [FromQuery] int? categoryId,
            [FromQuery] string? keyword,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _dishService.GetDishesAsync(branchId, categoryId, keyword, pageNumber, pageSize);
                return Ok(new ApiResponse<object>(200, "Dishes retrieved successfully", result));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Update a dish (Vendor only - must own the branch)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateDish(int id, [FromBody] UpdateDishRequest request)
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

                var result = await _dishService.UpdateDishAsync(id, request, userId);
                return Ok(new ApiResponse<DishResponse>(200, "Dish updated successfully", result));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }

        /// <summary>
        /// Delete a dish (Vendor only - must own the branch)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteDish(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new ApiResponse<object>(401, "User not authenticated", "UNAUTHORIZED"));
                }

                await _dishService.DeleteDishAsync(id, userId);
                return Ok(new ApiResponse<object>(200, "Dish deleted successfully", null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>(400, ex.Message, null));
            }
        }
    }
}
