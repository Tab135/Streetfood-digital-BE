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
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateSystemCampaign([FromBody] CreateCampaignDto dto)
        {
            await _campaignService.CreateSystemCampaignAsync(dto);
            return Ok(new { message = "Campaign created successfully" });
        }

        [HttpPost("branch/{branchId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> CreateRestaurantCampaign(int branchId, [FromBody] CreateCampaignDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            
            await _campaignService.CreateRestaurantCampaignAsync(userId, branchId, dto);
            return Ok(new { message = "Restaurant campaign created successfully" });
        }

                        [HttpPost("vendor")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> CreateVendorCampaign([FromBody] CreateCampaignDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _campaignService.CreateVendorCampaignAsync(userId, dto);
            return Ok(new { message = "Vendor campaign created successfully" });
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
    }
}

