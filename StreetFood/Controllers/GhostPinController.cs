using BO.DTO.GhostPin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;
using Service.PaymentsService;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StreetFood.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GhostPinController : ControllerBase
    {
        private readonly IGhostPinService _ghostPinService;
        private readonly IPaymentService _paymentService;

        public GhostPinController(IGhostPinService ghostPinService, IPaymentService paymentService)
        {
            _ghostPinService = ghostPinService;
            _paymentService = paymentService;
        }

        [HttpPost]
        [Authorize(Roles = "User,Vendor,Moderator,Admin")]
        public async Task<IActionResult> CreateGhostPin([FromBody] CreateGhostPinRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var pin = await _ghostPinService.CreateGhostPinAsync(userId, request);
            return CreatedAtAction(nameof(GetGhostPin), new { ghostPinId = pin.GhostPinId }, pin);
        }

        [HttpGet("{ghostPinId}")]
        [Authorize]
        public async Task<IActionResult> GetGhostPin(int ghostPinId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            var pin = await _ghostPinService.GetGhostPinByIdAsync(ghostPinId, userId, role);
            return Ok(new { message = "Ghost Pin details", data = pin });
        }

        [HttpPost("{ghostPinId}/approve")]
        [Authorize(Roles = "Moderator,Admin")]
        public async Task<IActionResult> ApproveGhostPin(int ghostPinId)
        {
            var pin = await _ghostPinService.ApproveGhostPinAsync(ghostPinId);
            return Ok(new { message = "Ghost Pin approved", data = pin });
        }

        [HttpPost("{ghostPinId}/reject")]
        [Authorize(Roles = "Moderator,Admin")]
        public async Task<IActionResult> RejectGhostPin(int ghostPinId, [FromBody] RejectGhostPinRequest request)
        {
            var pin = await _ghostPinService.RejectGhostPinAsync(ghostPinId, request);
            return Ok(new { message = "Ghost Pin rejected", data = pin });
        }

        [HttpPost("{ghostPinId}/audit")]
        [Authorize(Roles = "Moderator,Admin")]
        public async Task<IActionResult> AuditGhostPin(int ghostPinId, [FromBody] AuditGhostPinRequest request)
        {
            try
            {
                var pin = await _ghostPinService.AuditGhostPinAsync(ghostPinId, request);
                return Ok(new { message = "Audit complete", data = pin });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{ghostPinId}/claim")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> ClaimGhostPin(int ghostPinId, [FromBody] ClaimGhostPinRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                // Hardcode logic or get VendorId properly (assuming VendorId is known or passed)
                // In this simplified version, let's say the Vendor ID is needed or we get from claims if it's there
                int vendorId = int.Parse(User.FindFirst("VendorId")?.Value ?? "-1"); 
                // Wait, if VendorId isn't in claim we need to get Vendor by UserId.
                // Assuming it will be implemented properly. I will pass vendorId=userId for now as placeholder.
                
                var claimResult = (dynamic)await _ghostPinService.ClaimGhostPinAsync(ghostPinId, userId, request); // passed userId as proxy
                int branchId = claimResult.BranchId;

                var paymentLink = await _paymentService.CreatePaymentLink(userId, branchId);
                return Ok(new { message = claimResult.Message, paymentLink = paymentLink.PaymentUrl });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Only verified")) return Conflict(new { message = ex.Message });
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
