using BO.DTO.Payments;
using BO.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.PaymentsService
{
    public class PaymentService : IPaymentService
    {
        private const int SUBSCRIPTION_AMOUNT = 20000; // VND — hardcoded vendor registration fee
        private const int SUBSCRIPTION_DURATION_DAYS = 30;

        private readonly IPaymentRepository _paymentRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly PayOSClient _payOS;

        public PaymentService(
            IPaymentRepository paymentRepo,
            IBranchRepository branchRepo,
            IUserRepository userRepo,
            IConfiguration configuration,
            ILogger<PaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _branchRepo = branchRepo;
            _userRepo = userRepo;
            _configuration = configuration;
            _logger = logger;

            // Initialize PayOS SDK
            string clientId = _configuration["PayOS:ClientId"] ?? string.Empty;
            string apiKey = _configuration["PayOS:ApiKey"] ?? string.Empty;
            string checksumKey = _configuration["PayOS:ChecksumKey"] ?? string.Empty;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(checksumKey))
            {
                _logger.LogError("PayOS credentials are missing in configuration!");
                throw new InvalidOperationException("PayOS credentials not configured");
            }

            _payOS = new PayOSClient(clientId, apiKey, checksumKey);
            _logger.LogInformation("PayOS Client initialized successfully");
        }

        /// <summary>
        /// Creates a 20,000 VND PayOS checkout link for the vendor subscription fee.
        /// The branch must have been approved by a moderator before payment.
        /// </summary>
        public async Task<PaymentLinkResult> CreatePaymentLink(int userId, int branchId)
        {
            try
            {
                // 1. Load branch
                var branch = await _branchRepo.GetByIdAsync(branchId);
                if (branch == null)
                {
                    return new PaymentLinkResult { Success = false, Message = "Chi nhánh không tồn tại." };
                }

                // 2. Verify the user owns this branch
                if (branch.UserId != userId)
                {
                    return new PaymentLinkResult { Success = false, Message = "Bạn không có quyền thanh toán cho chi nhánh này." };
                }

                // 3. Check that the moderator has approved the branch register request
                var request = await _branchRepo.GetBranchRegisterRequestAsync(branchId);
                if (request == null || request.Status != RegisterVendorStatusEnum.Accept)
                {
                    return new PaymentLinkResult
                    {
                        Success = false,
                        Message = "Chi nhánh chưa được moderator xét duyệt. Vui lòng chờ phê duyệt trước khi thanh toán."
                    };
                }

                // 4. If subscription is still active, reject duplicate payment
                if (branch.SubscriptionExpiresAt.HasValue && branch.SubscriptionExpiresAt.Value > DateTime.UtcNow)
                {
                    return new PaymentLinkResult
                    {
                        Success = false,
                        RequiresConfirmation = true,
                        Message = $"Chi nhánh đang có subscription đến {branch.SubscriptionExpiresAt.Value:dd/MM/yyyy}. Bạn có muốn gia hạn sớm?"
                    };
                }

                // 5. Generate unique order code (timestamp-based)
                int timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int random = new Random().Next(100, 999);
                long orderCode = long.Parse($"{timestamp}{random}");

                if (orderCode > int.MaxValue)
                    orderCode = timestamp;

                while (await _paymentRepo.OrderCodeExists(orderCode))
                {
                    random = new Random().Next(100, 999);
                    orderCode = long.Parse($"{timestamp}{random}");
                    if (orderCode > int.MaxValue) { orderCode = timestamp; break; }
                }

                // 6. Description (max 25 chars for PayOS)
                const string description = "Dang ky Vendor 30 ngay";

                // 7. Create pending payment record
                var payment = await _paymentRepo.CreatePayment(
                    userId: userId,
                    orderCode: orderCode,
                    branchId: branchId,
                    amount: SUBSCRIPTION_AMOUNT,
                    description: description
                );

                // 8. Build PayOS payment link
                var returnUrl = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:4000/Payment/success";
                var cancelUrl = _configuration["PayOS:CancelUrl"] ?? "http://localhost:4000/Payment/cancel";

                var paymentData = new CreatePaymentLinkRequest
                {
                    OrderCode = (int)orderCode,
                    Amount = SUBSCRIPTION_AMOUNT,
                    Description = description,
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl
                };

                var paymentLinkResponse = await _payOS.PaymentRequests.CreateAsync(paymentData);

                // 9. Persist PayOS details
                await _paymentRepo.UpdatePaymentWithPayOSDetails(
                    orderCode: orderCode,
                    status: "PENDING",
                    paymentLinkId: paymentLinkResponse.PaymentLinkId,
                    checkoutUrl: paymentLinkResponse.CheckoutUrl);

                _logger.LogInformation(
                    "Payment link created: OrderCode={OrderCode}, UserId={UserId}, BranchId={BranchId}, Amount={Amount}",
                    orderCode, userId, branchId, SUBSCRIPTION_AMOUNT);

                return new PaymentLinkResult
                {
                    Success = true,
                    PaymentUrl = paymentLinkResponse.CheckoutUrl,
                    OrderCode = orderCode,
                    PaymentLinkId = paymentLinkResponse.PaymentLinkId,
                    Message = "Tạo link thanh toán thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment link for UserId={UserId}, BranchId={BranchId}", userId, branchId);
                return new PaymentLinkResult { Success = false, Message = "Có lỗi xảy ra khi tạo link thanh toán." };
            }
        }

        public async Task<Payment?> GetPaymentByOrderCode(long orderCode)
        {
            return await _paymentRepo.GetPaymentByOrderCode(orderCode);
        }

        public async Task<List<Payment>> GetUserPaymentHistory(int userId)
        {
            return await _paymentRepo.GetUserPayments(userId);
        }

        public async Task<PaymentStatusResponse> GetPaymentStatus(long orderCode)
        {
            try
            {
                var payment = await _paymentRepo.GetPaymentByOrderCode(orderCode);
                if (payment == null)
                    throw new Exception($"Payment not found for OrderCode: {orderCode}");

                // Try sync with PayOS if still PENDING
                if (payment.Status == "PENDING")
                {
                    try
                    {
                        var paymentInfo = await _payOS.PaymentRequests.GetAsync((int)orderCode);
                        if (paymentInfo?.Status != null)
                        {
                            string newStatus = paymentInfo.Status.ToString();
                            if (newStatus == "PAID" || newStatus == "CANCELLED")
                            {
                                DateTime? paidAt = newStatus == "PAID" ? DateTime.UtcNow : null;
                                payment = await _paymentRepo.UpdatePaymentFromWebhook(
                                    orderCode, newStatus,
                                    paymentInfo.Id?.ToString(),
                                    paidAt, "QR Code");

                                if (newStatus == "PAID" && payment != null)
                                    await ActivateVendorSubscriptionAsync(payment);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not sync payment status from PayOS for OrderCode={OrderCode}", orderCode);
                    }
                }

                return BuildStatusResponse(payment!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for OrderCode={OrderCode}", orderCode);
                throw;
            }
        }

        public async Task<PaymentStatusResponse> ConfirmPaymentFromRedirect(long orderCode, string status, string? transactionId)
        {
            try
            {
                var payment = await _paymentRepo.GetPaymentByOrderCode(orderCode);
                if (payment == null)
                    throw new Exception($"Payment not found for OrderCode: {orderCode}");

                if (payment.Status != "PENDING")
                    return BuildStatusResponse(payment);

                var payosPaymentInfo = await _payOS.PaymentRequests.GetAsync((int)orderCode);
                if (payosPaymentInfo == null)
                    throw new Exception($"Payment not found in PayOS: OrderCode={orderCode}");

                var actualStatus = payosPaymentInfo.Status.ToString().ToUpper();

                _logger.LogInformation(
                    "PayOS returned: OrderCode={OrderCode}, ActualStatus={ActualStatus}",
                    orderCode, actualStatus);

                if (actualStatus == "PAID")
                {
                    payment = await _paymentRepo.UpdatePaymentFromWebhook(
                        orderCode, "PAID",
                        transactionId ?? payosPaymentInfo.Id?.ToString(),
                        DateTime.UtcNow, "QR Code");

                    if (payment != null)
                        await ActivateVendorSubscriptionAsync(payment);
                }
                else if (actualStatus is "CANCELLED" or "EXPIRED")
                {
                    payment = await _paymentRepo.UpdatePaymentFromWebhook(
                        orderCode, actualStatus, null, null, null);
                }

                return BuildStatusResponse(payment!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment from redirect for OrderCode={OrderCode}", orderCode);
                throw;
            }
        }

        public async Task<bool> VerifyPaymentOwnership(long orderCode, int userId)
        {
            var payment = await _paymentRepo.GetPaymentByOrderCode(orderCode);
            return payment?.UserId == userId;
        }

        public async Task<bool> RegisterWebhookUrl(string webhookUrl)
        {
            try
            {
                await _payOS.Webhooks.ConfirmAsync(webhookUrl);
                _logger.LogInformation("Webhook URL registered: {WebhookUrl}", webhookUrl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register webhook URL: {WebhookUrl}", webhookUrl);
                return false;
            }
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// On successful payment:
        ///   • Sets branch.IsVerified = true
        ///   • Sets branch.IsSubscribed = true
        ///   • Sets branch.SubscriptionExpiresAt = now + 30 days
        ///   • Upgrades user.Role to Vendor (3)
        /// </summary>
        private async Task ActivateVendorSubscriptionAsync(Payment payment)
        {
            if (!payment.BranchId.HasValue)
            {
                _logger.LogWarning("Payment {Id} has no BranchId — skipping vendor activation", payment.Id);
                return;
            }

            var branch = await _branchRepo.GetByIdAsync(payment.BranchId.Value);
            if (branch == null)
            {
                _logger.LogWarning("Branch {BranchId} not found for payment {Id}", payment.BranchId.Value, payment.Id);
                return;
            }

            // verification is handled by moderator separately; payment marks subscription and activates
            branch.IsActive = true;
            branch.IsSubscribed = true;
            branch.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(SUBSCRIPTION_DURATION_DAYS);
            await _branchRepo.UpdateAsync(branch);

            // Promote user to Vendor role
            var user = await _userRepo.GetUserById(payment.UserId);
            if (user != null && user.Role != Role.Vendor)
            {
                user.Role = Role.Vendor;
                await _userRepo.UpdateAsync(user);
                _logger.LogInformation(
                    "User {UserId} promoted to Vendor after payment for Branch {BranchId}",
                    user.Id, branch.BranchId);
            }

            _logger.LogInformation(
                "Branch {BranchId} subscription activated until {ExpiresAt}",
                branch.BranchId, branch.SubscriptionExpiresAt);
        }

        private static PaymentStatusResponse BuildStatusResponse(Payment payment) =>
            new()
            {
                OrderCode = payment.OrderCode,
                Amount = payment.Amount,
                Status = payment.Status,
                Description = payment.Description,
                CreatedAt = payment.CreatedAt,
                PaidAt = payment.PaidAt,
                TransactionCode = payment.TransactionCode
            };

        private static string DeterminePaymentMethod(WebhookData data)
        {
            if (!string.IsNullOrEmpty(data.CounterAccountBankName))
                return $"Bank Transfer - {data.CounterAccountBankName}";
            if (!string.IsNullOrEmpty(data.VirtualAccountNumber))
                return "Virtual Account";
            return "QR Code";
        }
    }
}
