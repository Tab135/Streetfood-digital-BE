using BO.Common;
using BO.DTO.Dish;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using StreetFood.Services;
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
        private readonly IS3Service _s3Service;

        public DishController(IDishService dishService, IS3Service s3Service)
        {
            _dishService = dishService ?? throw new ArgumentNullException(nameof(dishService));
            _s3Service = s3Service ?? throw new ArgumentNullException(nameof(s3Service));
        }

        [HttpPost("vendor/{vendorId}")]
        [Authorize(Roles = "Vendor,Manager")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<DishResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateDish([FromRoute] int vendorId, [FromForm] CreateDishRequest request, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Model is not valid" });
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest(new { message = "Dish image is required" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var imageUrl = await _s3Service.UploadFileAsync(imageFile, "dishes");

            var result = await _dishService.CreateDishAsync(vendorId, request, userId, imageUrl);
            return CreatedAtAction(nameof(GetDishById), new { id = result.DishId }, result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<DishResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDishById(int id)
        {
            var result = await _dishService.GetDishByIdAsync(id);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<DishResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDishes(
            [FromRoute] int branchId,
            [FromQuery] int? categoryId,
            [FromQuery] string? keyword,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _dishService.GetDishesByBranchAsync(branchId, categoryId, keyword, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("vendor/{vendorId}")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<DishResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDishesByVendor(
            [FromRoute] int vendorId,
            [FromQuery] int? categoryId,
            [FromQuery] string? keyword,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _dishService.GetDishesByVendorAsync(vendorId, categoryId, keyword, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Vendor,Manager")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<DishResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateDish(int id, [FromForm] UpdateDishRequest request, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Model is not valid" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            string? imageUrl = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                imageUrl = await _s3Service.UploadFileAsync(imageFile, "dishes");
            }

            var result = await _dishService.UpdateDishAsync(id, request, userId, imageUrl);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Vendor,Manager")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteDish(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            await _dishService.DeleteDishAsync(id, userId);
            return Ok("Dish deleted successfully");
        }

        [HttpPost("branch/{branchId}")]
        [Authorize(Roles = "Vendor,Manager")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddDishesToBranch([FromBody] AssignDishesRequest request, [FromRoute] int branchId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "User not authenticated" });

            await _dishService.AddDishesToBranchAsync(request.DishIds, branchId, userId);
            return Ok(new { message = "Dishes assigned to branch successfully" });
        }

        [HttpDelete("branch/{branchId}")]
        [Authorize(Roles = "Vendor,Manager")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveDishesFromBranch([FromBody] AssignDishesRequest request, [FromRoute] int branchId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "User not authenticated" });

            await _dishService.RemoveDishesFromBranchAsync(request.DishIds, branchId, userId);
            return Ok(new { message = "Dishes removed from branch successfully" });
        }

        [HttpPatch("{dishId}/branch/{branchId}/availability")]
        [Authorize(Roles = "Vendor,Manager")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateDishAvailability(
            [FromRoute] int dishId,
            [FromRoute] int branchId,
            [FromBody] UpdateDishAvailabilityRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "User not authenticated" });

            await _dishService.UpdateDishAvailabilityAsync(dishId, branchId, request.IsSoldOut, userId);
            return Ok(new { message = "Dish availability updated successfully" });
        }

    }
}

