using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    /// <summary>
    /// Admin-only endpoints for reading and updating system settings at runtime.
    /// Changes take effect immediately — no restart required.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SettingController : ControllerBase
    {
        private readonly ISettingService _settingService;

        public SettingController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        /// <summary>Returns all settings currently loaded in memory.</summary>
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(new { data = _settingService.GetAll() });
        }

        /// <summary>Updates a setting by name. The new value is persisted to the DB
        /// and reflected in memory immediately.</summary>
        [HttpPatch("{name}")]
        public async Task<IActionResult> Update(string name, [FromBody] UpdateSettingRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _settingService.UpdateAsync(name, request.Value);
                return Ok(new
                {
                    message = $"Setting '{name}' updated to '{request.Value}' successfully.",
                    data = new { name, value = request.Value }
                });
            }
            catch (System.Collections.Generic.KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>Force-reload all settings from the database (useful after manual SQL edits).</summary>
        [HttpPost("reload")]
        public async Task<IActionResult> Reload()
        {
            await _settingService.ReloadAsync();
            return Ok(new { message = "Settings reloaded from database.", data = _settingService.GetAll() });
        }
    }

    public class UpdateSettingRequest
    {
        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
