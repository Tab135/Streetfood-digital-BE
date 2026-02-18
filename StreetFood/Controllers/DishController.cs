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

        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CreateDish([FromBody] CreateDishRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new {message = "Model is not valid" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new {message = "User not authenticated"});
            }

            var result = await _dishService.CreateDishAsync(request, userId);
            return CreatedAtAction(nameof(GetDishById), new { id = result.DishId }, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDishById(int id)
        {
            var result = await _dishService.GetDishByIdAsync(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDishes(
            [FromQuery] int? branchId,
            [FromQuery] int? categoryId,
            [FromQuery] string? keyword,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _dishService.GetDishesAsync(branchId, categoryId, keyword, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateDish(int id, [FromBody] UpdateDishRequest request)
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

            var result = await _dishService.UpdateDishAsync(id, request, userId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> DeleteDish(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest(new { message = "Model is not valid" });
            }

            await _dishService.DeleteDishAsync(id, userId);
            return Ok("Dish deleted successfully");
        }
    }
}
