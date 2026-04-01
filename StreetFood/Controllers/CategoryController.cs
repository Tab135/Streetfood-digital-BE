using BO.Common;
using BO.DTO.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using StreetFood.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IS3Service _s3Service;

        public CategoryController(ICategoryService categoryService, IS3Service s3Service)
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _s3Service = s3Service ?? throw new ArgumentNullException(nameof(s3Service));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromForm] CreateCategoryDto createDto, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Model is not valid" });

            if (imageFile == null || imageFile.Length == 0)
                return BadRequest(new { message = "Category image is required" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest(new { message = "Model is not valid" });
            }

            var imageUrl = await _s3Service.UploadFileAsync(imageFile, "categories");
            var created = await _categoryService.CreateCategoryAsync(createDto, userId, imageUrl);
            return CreatedAtAction(nameof(GetById), new { id = created.CategoryId }, created);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound(new { message = "Category not found" });

            return Ok(category);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var list = await _categoryService.GetAllCategoriesAsync();
            return Ok(list);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateCategoryDto updateDto, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Model is not valid" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest(new { message = "Model is not valid" });
            }

            string? imageUrl = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                imageUrl = await _s3Service.UploadFileAsync(imageFile, "categories");
            }

            var updated = await _categoryService.UpdateCategoryAsync(id, updateDto, userId, imageUrl);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest(new { message = "Model is not valid" });
            }

            await _categoryService.DeleteCategoryAsync(id, userId);
            return Ok( new { message = "Category deleted successfully" });
        }
    }
}
