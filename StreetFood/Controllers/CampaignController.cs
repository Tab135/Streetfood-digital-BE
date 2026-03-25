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
        public async Task<IActionResult> CreateRestaurantCampaign(int branchId, [FromBody] CreateVendorCampaignDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            
            var result = await _campaignService.CreateRestaurantCampaignAsync(userId, branchId, dto);
            return Ok(new { message = "Restaurant campaign created successfully", data = result });
        }

                        [HttpPost("vendor")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> CreateVendorCampaign([FromBody] CreateVendorCampaignDto dto)
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

        [HttpGet("system/joinable")]
        public async Task<IActionResult> GetJoinableSystemCampaigns([FromQuery] CampaignQueryDto query)
        {
            var result = await _campaignService.GetJoinableSystemCampaignsAsync(query);
            return Ok(new { message = "Lấy danh sách chiến dịch hệ thống cho phép tham gia thành công", data = result });
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicCampaigns([FromQuery] CampaignQueryDto query)
        {
            var result = await _campaignService.GetPublicCampaignsAsync(query);
            return Ok(new { message = "Lấy danh sách các chiến dịch đã public thành công", data = result });
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

        [HttpGet("branch/{branchId}")]
        [Authorize(Roles = "Vendor,Manager,Admin")]
        public async Task<IActionResult> GetCampaignsByBranchAsync(int branchId, [FromQuery] CampaignQueryDto query)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)!.Value;

            var result = await _campaignService.GetCampaignsByBranchAsync(userId, userRole, branchId, query);
            return Ok(new { message = "Lấy danh sách chiến dịch của chi nhánh thành công", data = result });
        }

        // ==================== CAMPAIGN IMAGE OPERATIONS ====================
        [HttpPost("{campaignId}/images")]
        [Authorize]
        public async Task<IActionResult> AddCampaignImage(int campaignId, List<IFormFile> images)
        {
            if (images == null || images.Count == 0)
                return BadRequest(new { message = "At least one image is required" });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)!.Value;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "campaigns");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var addedImages = new List<object>();
            foreach (var image in images)
            {
                if (image.Length == 0) continue;

                var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                var imageUrl = "http://159.223.47.89:5298" + $"/uploads/campaigns/{uniqueFileName}";
                var campaignImage = await _campaignService.AddCampaignImageAsync(campaignId, imageUrl, userId, userRole);
                addedImages.Add(campaignImage);
            }

            return Ok(new { message = "Images added successfully", data = addedImages });
        }

        [HttpGet("{campaignId}/images")]
        public async Task<IActionResult> GetCampaignImages(int campaignId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var images = await _campaignService.GetCampaignImagesAsync(campaignId, pageNumber, pageSize);
            return Ok(new { message = "Images retrieved successfully", data = images });
        }

        [HttpDelete("images/{imageId}")]
        [Authorize]
        public async Task<IActionResult> DeleteCampaignImage(int imageId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)!.Value;

            await _campaignService.DeleteCampaignImageAsync(imageId, userId, userRole);
            return Ok(new { message = "Image deleted successfully" });
        }
    }
}


