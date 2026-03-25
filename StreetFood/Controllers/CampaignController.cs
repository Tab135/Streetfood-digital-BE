using BO.DTO.Campaigns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampaignController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly Service.PaymentsService.IPaymentService _paymentService;

        public CampaignController(ICampaignService campaignService, Service.PaymentsService.IPaymentService paymentService)
        {
            _campaignService = campaignService;
            _paymentService = paymentService;
        }

        [HttpPost("system")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSystemCampaign([FromBody] CreateCampaignDto dto)
        {
            var result = await _campaignService.CreateSystemCampaignAsync(dto);
            return Ok(new { message = "Campaign created successfully", data = result });
        }

        [HttpPost("branch/{branchId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> CreateRestaurantCampaign(int branchId, [FromBody] CreateCampaignDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            
            var result = await _campaignService.CreateRestaurantCampaignAsync(userId, branchId, dto);
            return Ok(new { message = "Restaurant campaign created successfully", data = result });
        }

                        [HttpPost("vendor")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> CreateVendorCampaign([FromBody] CreateCampaignDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.CreateVendorCampaignAsync(userId, dto);
            return Ok(new { message = "Vendor campaign created successfully", data = result });
        }
                [HttpPost("join/system/{campaignId}/branch/{branchId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> JoinSystemCampaign(int campaignId, int branchId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            
            var branchCampaignId = await _campaignService.JoinSystemCampaignAsync(userId, branchId, campaignId);
            
            var paymentResult = await _paymentService.CreateCampaignPaymentLink(userId, branchId, branchCampaignId);
            
            if (paymentResult.Success)
            {
                return Ok(new
                {
                    message = "Joined successfully. Please proceed to payment.",
                    data = paymentResult
                });
            }

            return BadRequest(new { message = paymentResult.Message });
        }

                [HttpGet("system")]
        public async Task<IActionResult> GetSystemCampaigns([FromQuery] CampaignQueryDto query)
        {
            var result = await _campaignService.GetSystemCampaignsAsync(query);
            return Ok(new { message = "Lấy danh sách chiến dịch hệ thống thành công", data = result });
        }

        [HttpGet("vendor")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetVendorCampaigns([FromQuery] CampaignQueryDto query)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.GetVendorCampaignsAsync(userId, query);
            return Ok(new { message = "Lấy danh sách chiến dịch của vendor thành công", data = result });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCampaignById(int id)
        {
            var result = await _campaignService.GetCampaignByIdAsync(id);
            return Ok(new { message = "Lấy thông tin chiến dịch thành công", data = result });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCampaign(int id, [FromBody] UpdateCampaignDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)!.Value;

            var result = await _campaignService.UpdateCampaignAsync(userId, userRole, id, dto);
            return Ok(new { message = "Cập nhật chiến dịch thành công", data = result });
        }
    }
}

