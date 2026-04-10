using BO.Common;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TierController : ControllerBase
    {
        private readonly ITierService _tierService;

        public TierController(ITierService tierService)
        {
            _tierService = tierService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTiers()
        {
            var result = await _tierService.GetAllTiersAsync();
            return Ok(new { message = "Lấy danh sách Tier thành công", data = result });
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByIdAsync(int tierId)
        {
            var result = await _tierService.GetByIdAsync(tierId);
            return Ok(new { message = "Lấy Tier thành công", data = result });
        }
    }
}