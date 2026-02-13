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

        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create([FromBody] CreateTasteDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            var created = await _tasteService.CreateTasteAsync(createDto, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.TasteId }, created);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var taste = await _tasteService.GetTasteByIdAsync(id);
            if (taste == null)
                return NotFound("Taste not found");

            return Ok(taste);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _tasteService.GetAllTastesAsync();
            return Ok(list);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTasteDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            var updated = await _tasteService.UpdateTasteAsync(id, updateDto, userId);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("User not authenticated");
            }

            await _tasteService.DeleteTasteAsync(id, userId);
            return Ok("Taste deleted successfully");
        }
    }
}
