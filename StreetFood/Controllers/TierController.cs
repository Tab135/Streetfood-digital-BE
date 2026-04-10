using BO.Common;
using BO.DTO.Tier;
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
        [ProducesResponseType(typeof(ApiResponse<List<TierResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTiers()
        {
            var result = await _tierService.GetAllTiersAsync();
            return Ok(new { message = "Lấy danh sách Tier thành công", data = result });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<TierResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var result = await _tierService.GetByIdAsync(id);
            return Ok(new { message = "Lấy Tier thành công", data = result });
        }
    }
}