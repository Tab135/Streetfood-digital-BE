using BO.DTO.Campaigns;
using BO.Common;
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

        private static VendorJoinSystemCampaignPaymentResponseDto ToPaymentResponse(VendorJoinSystemCampaignResultDto result)
        {
            var response = new VendorJoinSystemCampaignPaymentResponseDto();
            if (result?.Branches == null) return response;

            foreach (var b in result.Branches)
            {
                response.Branches.Add(new VendorJoinSystemCampaignBranchStatusDto
                {
                    BranchId = b.BranchId,
                    Status = b.Status
                });
            }

            var paymentSource = result.Branches.Find(b => !string.IsNullOrWhiteSpace(b.PaymentUrl));
            if (paymentSource != null)
            {
                response.Payment = new VendorJoinSystemCampaignPaymentInfoDto
                {
                    PaymentUrl = paymentSource.PaymentUrl,
                    QrCode = paymentSource.QrCode,
                    OrderCode = paymentSource.OrderCode,
                    PaymentLinkId = paymentSource.PaymentLinkId
                };
            }

            return response;
        }

        [HttpPost("system")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSystemCampaign([FromBody] CreateCampaignDto dto)
        {
            var result = await _campaignService.CreateSystemCampaignAsync(dto);
            return Ok(new { message = "Campaign created successfully", data = result });
        }

        [HttpPost("vendor")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> CreateVendorCampaign([FromBody] CreateVendorCampaignDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.CreateVendorCampaignAsync(userId, dto);
            return Ok(new { message = "Vendor campaign created successfully", data = result });
        }
        // Legacy route (kept for backward compatibility), hide from Swagger/UI.
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("join/system/{campaignId}/branch/{branchId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> JoinSystemCampaign(int campaignId, int branchId, [FromBody] JoinSystemCampaignBranchesRequestDto? request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // If caller sends BranchIds in body -> batch join + 1 payment link
            if (request?.BranchIds != null && request.BranchIds.Count > 0)
            {
                var result = await _campaignService.VendorJoinSystemCampaignForBranchesAsync(userId, campaignId, request.BranchIds);
                return Ok(new { message = "Đã tạo yêu cầu tham gia và link thanh toán cho các chi nhánh được chọn", data = result });
            }

            // Backward-compatible: if no body, treat path branchId as single-item selection
            var singleResult = await _campaignService.VendorJoinSystemCampaignForBranchesAsync(userId, campaignId, new() { branchId });
            return Ok(new { message = "Đã tạo yêu cầu tham gia và link thanh toán cho các chi nhánh được chọn", data = singleResult });
        }

        // NEW primary route: branchIds only in body (no branchId variable in URL)
        [HttpPost("join/system/{campaignId}/branch")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> JoinSystemCampaignByBody(int campaignId, [FromBody] JoinSystemCampaignBranchesRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.VendorJoinSystemCampaignForBranchesAsync(userId, campaignId, request?.BranchIds ?? new());
            return Ok(new
            {
                message = "Đã tạo yêu cầu tham gia và link thanh toán cho các chi nhánh được chọn",
                data = ToPaymentResponse(result)
            });
        }

        [HttpGet("system")]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Vendor, Admin")]
        public async Task<IActionResult> GetVendorCampaigns([FromQuery] CampaignQueryDto query)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.GetVendorCampaignsAsync(userId, query);
            return Ok(new { message = "Lấy danh sách chiến dịch của vendor thành công", data = result });
        }

        /// <summary>Danh sách chi nhánh đang tham gia campaign do vendor tạo (theo BranchCampaign).</summary>
        [HttpGet("vendor/{campaignId}/branches")]
        [Authorize(Roles = "Vendor, Admin")]
        public async Task<IActionResult> GetVendorCampaignBranches(int campaignId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.GetVendorCampaignBranchesAsync(userId, campaignId);
            return Ok(new { message = "Lấy danh sách chi nhánh tham gia campaign thành công", data = result });
        }

        [HttpPost("vendor/{campaignId}/branches/add")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> AddBranchesToVendorCampaign(int campaignId, [FromBody] VendorCampaignBranchIdsDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.AddBranchesToVendorCampaignAsync(userId, campaignId, dto?.BranchIds ?? new());
            return Ok(new { message = "Đã thêm chi nhánh vào campaign", data = result });
        }

        [HttpPost("vendor/{campaignId}/branches/remove")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> RemoveBranchesFromVendorCampaign(int campaignId, [FromBody] VendorCampaignBranchIdsDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.RemoveBranchesFromVendorCampaignAsync(userId, campaignId, dto?.BranchIds ?? new());
            return Ok(new { message = "Đã gỡ chi nhánh khỏi campaign", data = result });
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

        // NEW: Get system campaign detail with eligible branches
        [HttpGet("system/{campaignId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetSystemCampaignDetailWithJoinableBranches(int campaignId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.GetSystemCampaignDetailWithJoinableBranchesAsync(userId, campaignId);
            return Ok(new { message = "Lấy chi tiết chiến dịch hệ thống thành công", data = result });
        }

        // NEW: Vendor join system campaign for all eligible branches
        // Legacy route with query param; keep but hide from Swagger/UI.
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("vendor/join")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> VendorJoinSystemCampaign([FromQuery] int campaignId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.VendorJoinSystemCampaignAsync(userId, campaignId);
            return Ok(new
            {
                message = "Đã tham gia chiến dịch hệ thống cho các chi nhánh hợp lệ",
                data = ToPaymentResponse(result)
            });
        }

        // New (preferred) route: campaignId in path to avoid missing query param in clients
        [HttpPost("vendor/join/{campaignId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> VendorJoinSystemCampaignByPath(int campaignId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _campaignService.VendorJoinSystemCampaignAsync(userId, campaignId);
            return Ok(new
            {
                message = "Đã tham gia chiến dịch hệ thống cho các chi nhánh hợp lệ",
                data = ToPaymentResponse(result)
            });
        }

        // ==================== CAMPAIGN IMAGE OPERATIONS ====================

        [HttpPost("{campaignId}/images")]
        [Authorize]
        public async Task<IActionResult> UpdateCampaignImage(int campaignId, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Image is required" });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)!.Value;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "campaigns");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            var imageUrl = "http://159.223.47.89:5298" + $"/uploads/campaigns/{uniqueFileName}";
            await _campaignService.UpdateCampaignImageUrlAsync(campaignId, imageUrl, userId, userRole);
            return Ok(new { message = "Image updated successfully", data = imageUrl });
        }


        [HttpGet("{campaignId}/images")]
        public async Task<IActionResult> GetCampaignImage(int campaignId)
        {
            var imageUrl = await _campaignService.GetCampaignImageUrlAsync(campaignId);
            return Ok(new { message = "Image retrieved successfully", data = imageUrl });
        }


        [HttpDelete("{campaignId}/image")]
        [Authorize]
        public async Task<IActionResult> DeleteCampaignImage(int campaignId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)!.Value;
            await _campaignService.UpdateCampaignImageUrlAsync(campaignId, null, userId, userRole);
            return Ok(new { message = "Image deleted successfully" });
        }
    }
}


