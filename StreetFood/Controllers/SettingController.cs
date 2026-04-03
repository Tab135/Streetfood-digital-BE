using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{

    [ApiController]
    [Route("api/[controller]")]

    public class SettingController : ControllerBase
    {
        private readonly ISettingService _settingService;

        public SettingController(ISettingService settingService)
        {
            _settingService = settingService;
        }


        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(new { data = _settingService.GetAll() });
        }

        [HttpPatch("{name}")]
        [Authorize(Roles = "Admin")]
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


        [HttpPost("reload")]
        [Authorize(Roles = "Admin")]
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
