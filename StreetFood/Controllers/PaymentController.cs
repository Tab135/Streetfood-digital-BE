using BO.Common;
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

        [HttpPost("create-link")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PaymentLinkResult>), StatusCodes.Status200OK)]
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
        [ProducesResponseType(typeof(ApiResponse<PaymentStatusResponse>), StatusCodes.Status200OK)]
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
        [ProducesResponseType(typeof(ApiResponse<List<Payment>>), StatusCodes.Status200OK)]
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
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
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
        [ProducesResponseType(typeof(ApiResponse<PaymentStatusResponse>), StatusCodes.Status200OK)]
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

        // [HttpPost("order/confirm")]
        // [Authorize(Roles = "User")]
        // public async Task<ActionResult<PaymentStatusResponse>> ConfirmOrderPayment([FromBody] ConfirmPaymentDto request)
        // {
        //     try
        //     {
        //         var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //         if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        //         {
        //             return Unauthorized(new { message = "User not authenticated" });
        //         }

        //         var paymentOwnerCheck = await _paymentService.VerifyPaymentOwnership(request.OrderCode, userId);
        //         if (!paymentOwnerCheck)
        //         {
        //             return Forbid();
        //         }

        //         var result = await _paymentService.ConfirmPaymentFromRedirect(
        //             request.OrderCode,
        //             request.Status ?? "",
        //             request.TransactionId);

        //         return Ok(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error confirming order payment for OrderCode={OrderCode}", request.OrderCode);
        //         return StatusCode(500, new { message = "Failed to confirm order payment" });
        //     }
        // }

        // [HttpPost("campaign/confirm")]
        // [Authorize(Roles = "Vendor")]
        // public async Task<ActionResult<PaymentStatusResponse>> ConfirmCampaignPayment([FromBody] ConfirmPaymentDto request)
        // {
        //     try
        //     {
        //         var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //         if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        //         {
        //             return Unauthorized(new { message = "User not authenticated" });
        //         }

        //         var paymentOwnerCheck = await _paymentService.VerifyPaymentOwnership(request.OrderCode, userId);
        //         if (!paymentOwnerCheck)
        //         {
        //             return Forbid();
        //         }

        //         var result = await _paymentService.ConfirmPaymentFromRedirect(
        //             request.OrderCode,
        //             request.Status ?? "",
        //             request.TransactionId);

        //         return Ok(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error confirming campaign payment for OrderCode={OrderCode}", request.OrderCode);
        //         return StatusCode(500, new { message = "Failed to confirm campaign payment" });
        //     }
        // }

        [HttpPost("webhook")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReceiveWebhook([FromBody] Webhook webhook)
        {
            try
            {
                var handled = await _paymentService.HandleWebhookAsync(webhook);
                if (!handled)
                {
                    return BadRequest(new { message = "Webhook processing failed" });
                }

                return Ok(new { message = "Webhook received" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS webhook");
                return StatusCode(500, new { message = "Failed to process webhook" });
            }
        }

        [HttpPost("webhook/register")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterWebhook([FromBody] RegisterWebhookDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WebhookUrl))
            {
                return BadRequest(new { message = "WebhookUrl is required" });
            }

            var success = await _paymentService.RegisterWebhookUrl(request.WebhookUrl);
            if (!success)
            {
                return BadRequest(new { message = "Failed to register webhook URL" });
            }

            return Ok(new { message = "Webhook URL registered successfully", data = new { request.WebhookUrl } });
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

        [HttpGet("user/balance")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetUserBalance()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var balance = await _paymentService.GetUserBalanceAsync(userId);
            return Ok(new { message = "Get user balance successfully", data = new { balance } });
        }

        [HttpPost("user/transfer")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> RequestUserTransfer([FromBody] VendorPayoutRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var result = await _paymentService.RequestUserPayoutAsync(userId, request);
                return Ok(new
                {
                    message = "Transfer request created successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user transfer request");
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpGet("vendor/balance")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetVendorBalance()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var balance = await _paymentService.GetVendorBalanceAsync(userId);
            return Ok(new { message = "Get vendor balance successfully", data = new { balance } });
        }

        [HttpPost("vendor/transfer")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> RequestVendorTransfer([FromBody] VendorPayoutRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var result = await _paymentService.RequestVendorPayoutAsync(userId, request);
                return Ok(new
                {
                    message = "Transfer request created successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor transfer request");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}