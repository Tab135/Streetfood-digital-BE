using BO.DTO.Payments;
using BO.Entities;
using BO.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models;
using PayOS.Models.V1.Payouts;
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
        private readonly PayOSClient _payoutClient;
        private readonly IVendorRepository _vendorRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly bool _isDebugMode;

        public PaymentService(
            IPaymentRepository paymentRepo,
            IBranchRepository branchRepo,
            IUserRepository userRepo,
            IVendorRepository vendorRepository,
            IOrderRepository orderRepository,
            IConfiguration configuration,
            ILogger<PaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _branchRepo = branchRepo;
            _userRepo = userRepo;
            _vendorRepository = vendorRepository;
            _orderRepository = orderRepository;
            _configuration = configuration;
            _logger = logger;
            _isDebugMode = bool.TryParse(_configuration["PayOS:DebugMode"], out var debugMode) && debugMode;

            if (_isDebugMode)
            {
                _payOS = new PayOSClient(new PayOSOptions
                {
                    ClientId = "debug",
                    ApiKey = "debug",
                    ChecksumKey = "debug",
                    LogLevel = LogLevel.Debug
                });

                _payoutClient = new PayOSClient(new PayOSOptions
                {
                    ClientId = "debug",
                    ApiKey = "debug",
                    ChecksumKey = "debug",
                    LogLevel = LogLevel.Debug
                });

                _logger.LogWarning("PayOS debug mode is enabled. External PayOS calls are bypassed.");
                return;
            }

            // Initialize PayOS SDK
            string clientId = _configuration["PayOS:ClientId"] ?? string.Empty;
            string apiKey = _configuration["PayOS:ApiKey"] ?? string.Empty;
            string checksumKey = _configuration["PayOS:ChecksumKey"] ?? string.Empty;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(checksumKey))
            {
                _logger.LogError("PayOS credentials are missing in configuration!");
                throw new InvalidOperationException("PayOS credentials not configured");
            }

            _payOS = new PayOSClient(new PayOSOptions
            {
                ClientId = clientId,
                ApiKey = apiKey,
                ChecksumKey = checksumKey,
                LogLevel = LogLevel.Debug
            });

            var payoutClientId = _configuration["PayOS:PayoutClientId"] ?? string.Empty;
            var payoutApiKey = _configuration["PayOS:PayoutApiKey"] ?? string.Empty;
            var payoutChecksumKey = _configuration["PayOS:PayoutChecksumKey"] ?? string.Empty;

            if (string.IsNullOrEmpty(payoutClientId) || string.IsNullOrEmpty(payoutApiKey) || string.IsNullOrEmpty(payoutChecksumKey))
            {
                _logger.LogError("PayOS payout credentials are missing in configuration!");
                throw new InvalidOperationException("PayOS payout credentials not configured");
            }

            _payoutClient = new PayOSClient(new PayOSOptions
            {
                ClientId = payoutClientId,
                ApiKey = payoutApiKey,
                ChecksumKey = payoutChecksumKey,
                LogLevel = LogLevel.Debug
            });

            _logger.LogInformation("PayOS Client initialized successfully");
        }

        public async Task<decimal> GetVendorBalanceAsync(int vendorUserId)
        {
            var vendor = await _vendorRepository.GetByUserIdAsync(vendorUserId)
                ?? throw new DomainExceptions("Vendor not found");

            return vendor.MoneyBalance;
        }

        public async Task<decimal> GetUserBalanceAsync(int userId)
        {
            var user = await _userRepo.GetUserById(userId)
                ?? throw new DomainExceptions("User not found");

            return user.MoneyBalance;
        }

        public async Task<VendorPayoutResponseDto> RequestUserPayoutAsync(int userId, VendorPayoutRequestDto request)
        {
            var user = await _userRepo.GetUserById(userId)
                ?? throw new DomainExceptions("User not found");

            if (request.Amount <= 0)
            {
                throw new DomainExceptions("Amount must be greater than 0");
            }

            var amount = Convert.ToDecimal(request.Amount);
            if (user.MoneyBalance < amount)
            {
                throw new DomainExceptions("Insufficient user balance");
            }

            if (_isDebugMode)
            {
                user.MoneyBalance -= amount;
                await _userRepo.UpdateAsync(user);

                return new VendorPayoutResponseDto
                {
                    ReferenceId = $"DEBUG-USER-{Guid.NewGuid():N}",
                    PayoutId = $"DEBUG-PAYOUT-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                    ApprovalState = "APPROVED_DEBUG",
                    CurrentVendorBalance = user.MoneyBalance
                };
            }

            var payoutRequest = new PayoutRequest
            {
                ReferenceId = Guid.NewGuid().ToString(),
                Amount = request.Amount,
                Description = request.Description,
                ToBin = request.ToBin,
                ToAccountNumber = request.ToAccountNumber,
                Category = request.Category ?? new List<string>()
            };

            Payout payout;
            try
            {
                payout = await _payoutClient.Payouts.CreateAsync(payoutRequest);
            }
            catch (ForbiddenException ex)
            {
                _logger.LogError(ex,
                    "PayOS payout forbidden. Verify PayoutClientId/PayoutApiKey/PayoutChecksumKey and payout permission for this merchant.");
                throw new DomainExceptions("PayOS payout is forbidden. Please verify payout credentials and payout permission with PayOS.");
            }

            user.MoneyBalance -= amount;
            await _userRepo.UpdateAsync(user);

            return new VendorPayoutResponseDto
            {
                ReferenceId = payout.ReferenceId,
                PayoutId = payout.Id,
                ApprovalState = payout.ApprovalState.ToString(),
                CurrentVendorBalance = user.MoneyBalance
            };
        }

        public async Task<VendorPayoutResponseDto> RequestVendorPayoutAsync(int vendorUserId, VendorPayoutRequestDto request)
        {
            var vendor = await _vendorRepository.GetByUserIdAsync(vendorUserId)
                ?? throw new DomainExceptions("Vendor not found");

            if (request.Amount <= 0)
            {
                throw new DomainExceptions("Amount must be greater than 0");
            }

            var amount = Convert.ToDecimal(request.Amount);
            if (vendor.MoneyBalance < amount)
            {
                throw new DomainExceptions("Insufficient vendor balance");
            }

            if (_isDebugMode)
            {
                vendor.MoneyBalance -= amount;
                await _vendorRepository.UpdateAsync(vendor);

                return new VendorPayoutResponseDto
                {
                    ReferenceId = $"DEBUG-VENDOR-{Guid.NewGuid():N}",
                    PayoutId = $"DEBUG-PAYOUT-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                    ApprovalState = "APPROVED_DEBUG",
                    CurrentVendorBalance = vendor.MoneyBalance
                };
            }

            var payoutRequest = new PayoutRequest
            {
                ReferenceId = Guid.NewGuid().ToString(),
                Amount = request.Amount,
                Description = request.Description,
                ToBin = request.ToBin,
                ToAccountNumber = request.ToAccountNumber,
                Category = request.Category ?? new List<string>()
            };

            Payout payout;
            try
            {
                payout = await _payoutClient.Payouts.CreateAsync(payoutRequest);
            }
            catch (ForbiddenException ex)
            {
                _logger.LogError(ex,
                    "PayOS payout forbidden. Verify PayoutClientId/PayoutApiKey/PayoutChecksumKey and payout permission for this merchant.");
                throw new DomainExceptions("PayOS payout is forbidden. Please verify payout credentials and payout permission with PayOS.");
            }

            vendor.MoneyBalance -= amount;
            await _vendorRepository.UpdateAsync(vendor);

            return new VendorPayoutResponseDto
            {
                ReferenceId = payout.ReferenceId,
                PayoutId = payout.Id,
                ApprovalState = payout.ApprovalState.ToString(),
                CurrentVendorBalance = vendor.MoneyBalance
            };
        }

        public async Task<PaymentLinkResult> CreateOrderPaymentLink(int userId, int orderId)
        {
            try
            {
                var order = await _orderRepository.GetById(orderId);
                if (order == null)
                {
                    return new PaymentLinkResult { Success = false, Message = "Order not found" };
                }

                if (order.UserId != userId)
                {
                    return new PaymentLinkResult { Success = false, Message = "You do not own this order" };
                }

                if (order.Status != OrderStatus.Pending)
                {
                    return new PaymentLinkResult { Success = false, Message = "Only pending orders can be paid" };
                }

                var latestPayment = await _paymentRepo.GetLatestPaymentByOrderId(orderId);
                if (latestPayment != null)
                {
                    if (latestPayment.Status == "PAID")
                    {
                        return new PaymentLinkResult { Success = false, Message = "This order is already paid" };
                    }

                    if (latestPayment.Status == "PENDING" && !string.IsNullOrEmpty(latestPayment.CheckoutUrl))
                    {
                        return new PaymentLinkResult
                        {
                            Success = true,
                            Message = "Using existing pending payment link",
                            PaymentUrl = latestPayment.CheckoutUrl,
                            OrderCode = latestPayment.OrderCode,
                            PaymentLinkId = latestPayment.PaymentLinkId
                        };
                    }
                }

                var orderCode = await GenerateUniqueOrderCodeAsync();
                var amount = Convert.ToInt32(decimal.Round(order.FinalAmount, 0, MidpointRounding.AwayFromZero));
                if (amount <= 0)
                {
                    return new PaymentLinkResult { Success = false, Message = "Order amount must be greater than 0" };
                }

                var description = $"Order {order.OrderId}";

                await _paymentRepo.CreatePayment(
                    userId: userId,
                    orderCode: orderCode,
                    branchId: order.BranchId,
                    amount: amount,
                    description: description,
                    orderId: order.OrderId);

                var returnUrl = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:4000/Payment/success";
                var cancelUrl = _configuration["PayOS:CancelUrl"] ?? "http://localhost:4000/Payment/cancel";

                var paymentData = new CreatePaymentLinkRequest
                {
                    OrderCode = (int)orderCode,
                    Amount = amount,
                    Description = description,
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl
                };

                string paymentLinkId;
                string checkoutUrl;
                if (_isDebugMode)
                {
                    paymentLinkId = $"DEBUG-ORDER-{orderCode}";
                    checkoutUrl = $"{returnUrl}?debug=true&orderCode={orderCode}";
                }
                else
                {
                    var paymentLinkResponse = await _payOS.PaymentRequests.CreateAsync(paymentData);
                    paymentLinkId = paymentLinkResponse.PaymentLinkId;
                    checkoutUrl = paymentLinkResponse.CheckoutUrl;
                }

                await _paymentRepo.UpdatePaymentWithPayOSDetails(
                    orderCode: orderCode,
                    status: "PENDING",
                    paymentLinkId: paymentLinkId,
                    checkoutUrl: checkoutUrl);

                return new PaymentLinkResult
                {
                    Success = true,
                    Message = _isDebugMode ? "Debug payment link created" : "Create payment link successfully",
                    PaymentUrl = checkoutUrl,
                    OrderCode = orderCode,
                    PaymentLinkId = paymentLinkId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order payment link for UserId={UserId}, OrderId={OrderId}", userId, orderId);
                return new PaymentLinkResult { Success = false, Message = "Có lỗi xảy ra khi tạo link thanh toán order." };
            }
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

            var vendor = branch.VendorId.HasValue ? await _vendorRepository.GetByIdAsync(branch.VendorId.Value) : null; 
            if (vendor != null)
                {
                    var vendorOwner = await _userRepo.GetUserById(vendor.UserId);
                  // 2. Verify the user owns this branch
                    if (vendorOwner.Id != userId)
                    {
                        return new PaymentLinkResult { Success = false, Message = "Bạn không có quyền thanh toán cho chi nhánh này." };
                    }
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

                string paymentLinkId;
                string checkoutUrl;
                if (_isDebugMode)
                {
                    paymentLinkId = $"DEBUG-SUB-{orderCode}";
                    checkoutUrl = $"{returnUrl}?debug=true&orderCode={orderCode}";
                }
                else
                {
                    var paymentLinkResponse = await _payOS.PaymentRequests.CreateAsync(paymentData);
                    paymentLinkId = paymentLinkResponse.PaymentLinkId;
                    checkoutUrl = paymentLinkResponse.CheckoutUrl;
                }

                // 9. Persist PayOS details
                await _paymentRepo.UpdatePaymentWithPayOSDetails(
                    orderCode: orderCode,
                    status: "PENDING",
                    paymentLinkId: paymentLinkId,
                    checkoutUrl: checkoutUrl);

                _logger.LogInformation(
                    "Payment link created: OrderCode={OrderCode}, UserId={UserId}, BranchId={BranchId}, Amount={Amount}",
                    orderCode, userId, branchId, SUBSCRIPTION_AMOUNT);

                return new PaymentLinkResult
                {
                    Success = true,
                    PaymentUrl = checkoutUrl,
                    OrderCode = orderCode,
                    PaymentLinkId = paymentLinkId,
                    Message = _isDebugMode ? "Tạo link thanh toán debug thành công" : "Tạo link thanh toán thành công"
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
                if (payment.Status == "PENDING" && !_isDebugMode)
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
                                {
                                    if (payment.OrderId.HasValue)
                                    {
                                        await MoveOrderToVendorConfirmationAsync(payment.OrderId.Value);
                                    }
                                    else
                                    {
                                        await ActivateVendorSubscriptionAsync(payment);
                                    }
                                }
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

                var actualStatus = "PAID";
                string? externalTxnId = transactionId;

                if (!_isDebugMode)
                {
                    var payosPaymentInfo = await _payOS.PaymentRequests.GetAsync((int)orderCode);
                    if (payosPaymentInfo == null)
                        throw new Exception($"Payment not found in PayOS: OrderCode={orderCode}");

                    actualStatus = payosPaymentInfo.Status.ToString().ToUpper();
                    externalTxnId = transactionId ?? payosPaymentInfo.Id?.ToString();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(status))
                    {
                        actualStatus = status.ToUpper();
                    }

                    if (actualStatus == "SUCCESS")
                    {
                        actualStatus = "PAID";
                    }
                }

                _logger.LogInformation(
                    "PayOS returned: OrderCode={OrderCode}, ActualStatus={ActualStatus}",
                    orderCode, actualStatus);

                if (actualStatus == "PAID")
                {
                    payment = await _paymentRepo.UpdatePaymentFromWebhook(
                        orderCode, "PAID",
                        externalTxnId ?? $"DEBUG-TXN-{orderCode}",
                        DateTime.UtcNow, "QR Code");

                    if (payment != null)
                    {
                        if (payment.OrderId.HasValue)
                        {
                            await MoveOrderToVendorConfirmationAsync(payment.OrderId.Value);
                        }
                        else
                        {
                            await ActivateVendorSubscriptionAsync(payment);
                        }
                    }
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
                if (_isDebugMode)
                {
                    _logger.LogInformation("Skipping webhook registration in debug mode: {WebhookUrl}", webhookUrl);
                    return true;
                }

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

            // verification and activation are handled by moderator separately; payment marks subscription
            
            if (branch.VendorId == null || branch.VendorId == 0)
            {
                var vendor = await _vendorRepository.GetByUserIdAsync(payment.UserId);
                if (vendor == null)
                {
                    vendor = new BO.Entities.Vendor { UserId = payment.UserId };
                    vendor = await _vendorRepository.CreateAsync(vendor);
                }
                branch.VendorId = vendor.VendorId;
            }
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

        private async Task MoveOrderToVendorConfirmationAsync(int orderId)
        {
            var order = await _orderRepository.GetById(orderId);
            if (order == null)
            {
                throw new DomainExceptions("Order not found when confirming payment");
            }

            if (order.Status == OrderStatus.Pending)
            {
                var branch = await _branchRepo.GetByIdAsync(order.BranchId)
                    ?? throw new DomainExceptions("Branch not found when confirming payment");

                var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId)
                    ?? throw new DomainExceptions("Vendor not found when confirming payment");

                // Instead of adding balance here, it will be handled when the vendor decides (OrderService)
                order.Status = OrderStatus.AwaitingVendorConfirmation;
                await _orderRepository.Update(order);
            }
        }

        private async Task<long> GenerateUniqueOrderCodeAsync()
        {
            int timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int random = Random.Shared.Next(100, 999);
            long orderCode = long.Parse($"{timestamp}{random}");

            if (orderCode > int.MaxValue)
            {
                orderCode = timestamp;
            }

            while (await _paymentRepo.OrderCodeExists(orderCode))
            {
                random = Random.Shared.Next(100, 999);
                orderCode = long.Parse($"{timestamp}{random}");
                if (orderCode > int.MaxValue)
                {
                    orderCode = timestamp;
                    break;
                }
            }

            return orderCode;
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
