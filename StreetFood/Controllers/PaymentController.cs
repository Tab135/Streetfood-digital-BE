using BO.DTO.Payments;
using BO.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using Service.PaymentsService;
using System.Security.Claims;
using System.Text.Json;

namespace Ielts_System.Controllers.Payments
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Create a payment link for a branch vendor subscription (20,000 VND / 30 days).
        /// The branch must have been approved by a moderator first.
        /// Endpoint: POST /api/payment/create-link
        /// </summary>
        [HttpPost("create-link")]
        [Authorize]
        public async Task<ActionResult<PaymentLinkResult>> CreatePaymentLink([FromBody] CreatePaymentLinkDto request)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "User not authenticated" });

                if (request.BranchId <= 0)
                    return BadRequest(new { message = "Invalid BranchId" });

                var result = await _paymentService.CreatePaymentLink(userId, request.BranchId);

                if (result.RequiresConfirmation)
                    return Conflict(result);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link for UserId={UserId}, BranchId={BranchId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier), request.BranchId);
                return StatusCode(500, new { message = "An error occurred while creating payment link" });
            }
        }


        [HttpGet("status/{orderCode}")]
        [Authorize]
        public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatus(long orderCode)
        {
            try
            {
                var result = await _paymentService.GetPaymentStatus(orderCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for OrderCode={OrderCode}", orderCode);
                return StatusCode(500, new { message = "Failed to retrieve payment status" });
            }
        }


        [HttpGet("history")]
        [Authorize]
        public async Task<ActionResult<List<Payment>>> GetPaymentHistory()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var result = await _paymentService.GetUserPaymentHistory(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment history");
                return StatusCode(500, new { message = "Failed to retrieve payment history" });
            }
        }




        [HttpGet("success")]
        [AllowAnonymous]
        public IActionResult PaymentSuccess([FromQuery] long orderCode, [FromQuery] string? status)
        {
            _logger.LogInformation("Payment success redirect: OrderCode={OrderCode}, Status={Status}",
                orderCode, status);

            return Ok(new
            {
                message = "Payment completed successfully",
                orderCode = orderCode,
                status = status ?? "PAID"
            });
        }


        [HttpPost("confirm")]
        [Authorize]
        public async Task<ActionResult<PaymentStatusResponse>> ConfirmPayment([FromBody] ConfirmPaymentDto request)
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                _logger.LogInformation("Confirming payment: OrderCode={OrderCode}, Status={Status}, Code={Code}, UserId={UserId}",
                    request.OrderCode, request.Status, request.Code, userId);

                // SECURITY: Verify payment belongs to this user
                var paymentOwnerCheck = await _paymentService.VerifyPaymentOwnership(request.OrderCode, userId);
                if (!paymentOwnerCheck)
                {
                    _logger.LogWarning("Payment ownership mismatch: OrderCode={OrderCode}, UserId={UserId}",
                        request.OrderCode, userId);
                    return Forbid();
                }

                // Call service to verify with PayOS and update status
                // The service will verify with PayOS API, not trust URL parameters
                var result = await _paymentService.ConfirmPaymentFromRedirect(
                    request.OrderCode,
                    request.Status ?? "",
                    request.TransactionId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for OrderCode={OrderCode}", request.OrderCode);
                return StatusCode(500, new { message = "Failed to confirm payment" });
            }
        }

        [HttpGet("cancel")]
        [AllowAnonymous]
        public IActionResult PaymentCancel([FromQuery] long orderCode)
        {
            _logger.LogInformation("Payment cancelled: OrderCode={OrderCode}", orderCode);

            return Ok(new
            {
                message = "Payment was cancelled",
                orderCode = orderCode
            });
        }
    }
}