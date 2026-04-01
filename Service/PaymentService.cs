using BO.DTO.Notification;
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
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.PaymentsService
{
    public class PaymentService : IPaymentService
    {
        // Fees and durations are now loaded from the Settings table at runtime.
        // Keys match Setting.Name seeds: "SubscriptionFee", "SubscriptionDurationDays", "CampaignJoinFee".
        private int SubscriptionAmount      => _settings.GetInt("SubscriptionFee", 20000);
        private int SubscriptionDurationDays => _settings.GetInt("SubscriptionDurationDays", 30);
        private int CampaignJoinFee         => _settings.GetInt("CampaignJoinFee", 20000);

        private readonly IPaymentRepository _paymentRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly PayOSClient _payOS;
        private readonly PayOSClient _payoutClient;
        private readonly IVendorRepository _vendorRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IBranchCampaignRepository _branchCampaignRepo;
        private readonly ICartRepository _cartRepo;
        private readonly INotificationPusher _notificationPusher;
        private readonly ISettingService _settings;
        private readonly bool _isDebugMode;

        public PaymentService(
            IPaymentRepository paymentRepo,
            IBranchRepository branchRepo,
            IUserRepository userRepo,
            IVendorRepository vendorRepository,
            IOrderRepository orderRepository,
            IBranchCampaignRepository branchCampaignRepo,
            ICartRepository cartRepo,
            INotificationPusher notificationPusher,
            ISettingService settings,
            IConfiguration configuration,
            ILogger<PaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _branchRepo = branchRepo;
            _userRepo = userRepo;
            _vendorRepository = vendorRepository;
            _orderRepository = orderRepository;
            _branchCampaignRepo = branchCampaignRepo;
            _cartRepo = cartRepo;
            _notificationPusher = notificationPusher;
            _settings = settings;
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
                            QrCode = latestPayment.CheckoutUrl,
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

                var description = $"Thanh toan don hang {order.OrderId}";
                var payOsDescription = BuildPayOSDescription(description, "Thanh toan don hang");

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
                    Description = payOsDescription,
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl
                };

                string paymentLinkId;
                string checkoutUrl;
                string qrCode;
                if (_isDebugMode)
                {
                    paymentLinkId = $"DEBUG-ORDER-{orderCode}";
                    checkoutUrl = $"{returnUrl}?debug=true&orderCode={orderCode}";
                    qrCode = checkoutUrl;
                }
                else
                {
                    var paymentLinkResponse = await _payOS.PaymentRequests.CreateAsync(paymentData);
                    paymentLinkId = paymentLinkResponse.PaymentLinkId;
                    checkoutUrl = paymentLinkResponse.CheckoutUrl;
                    qrCode = paymentLinkResponse.QrCode;
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
                    QrCode = qrCode,
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
                var request = await _branchRepo.GetBranchRequestAsync(branchId);
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

                var description = $"Dang ky vendor {branch.Name} {SubscriptionDurationDays} ngay";
                var payOsDescription = BuildPayOSDescription(description, "Dang ky vendor 30 ngay");

                // 7. Create pending payment record
                var payment = await _paymentRepo.CreatePayment(
                    userId: userId,
                    orderCode: orderCode,
                    branchId: branchId,
                    amount: SubscriptionAmount,
                    description: description
                );

                // 8. Build PayOS payment link
                var returnUrl = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:4000/Payment/success";
                var cancelUrl = _configuration["PayOS:CancelUrl"] ?? "http://localhost:4000/Payment/cancel";

                var paymentData = new CreatePaymentLinkRequest
                {
                    OrderCode = (int)orderCode,
                    Amount = SubscriptionAmount,
                    Description = payOsDescription,
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl
                };

                string paymentLinkId;
                string checkoutUrl;
                string qrCode;
                if (_isDebugMode)
                {
                    paymentLinkId = $"DEBUG-SUB-{orderCode}";
                    checkoutUrl = $"{returnUrl}?debug=true&orderCode={orderCode}";
                    qrCode = checkoutUrl;
                }
                else
                {
                    var paymentLinkResponse = await _payOS.PaymentRequests.CreateAsync(paymentData);
                    paymentLinkId = paymentLinkResponse.PaymentLinkId;
                    checkoutUrl = paymentLinkResponse.CheckoutUrl;
                    qrCode = paymentLinkResponse.QrCode;
                }

                // 9. Persist PayOS details
                await _paymentRepo.UpdatePaymentWithPayOSDetails(
                    orderCode: orderCode,
                    status: "PENDING",
                    paymentLinkId: paymentLinkId,
                    checkoutUrl: checkoutUrl);

                _logger.LogInformation(
                    "Payment link created: OrderCode={OrderCode}, UserId={UserId}, BranchId={BranchId}, Amount={Amount}",
                    orderCode, userId, branchId, SubscriptionAmount);

                return new PaymentLinkResult
                {
                    Success = true,
                    PaymentUrl = checkoutUrl,
                    QrCode = qrCode,
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
                    throw new DomainExceptions($"Không tìm thấy thanh toán cho mã đơn hàng: {orderCode}");

                // Try sync with PayOS if still PENDING
                if (payment.Status == "PENDING" && !_isDebugMode)
                {
                    try
                    {
                        var paymentInfo = await _payOS.PaymentRequests.GetAsync((int)orderCode);
                        if (paymentInfo?.Status != null)
                        {
                            var newStatus = paymentInfo.Status.ToString().ToUpperInvariant();
                            if (newStatus is "PAID" or "CANCELLED" or "EXPIRED")
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
                        throw new DomainExceptions($"Không tìm thấy thanh toán trên PayOS: Mã đơn hàng={orderCode}");

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

                            var cart = await _cartRepo.GetByUserIdAsync(payment.UserId);
                            if (cart != null)
                            {
                                await _cartRepo.ClearItemsAsync(cart.CartId);
                                cart.BranchId = null;
                                await _cartRepo.UpdateAsync(cart);
                            }

                            await _notificationPusher.PushPaymentStatusAsync(
                                payment.UserId, orderCode, "PAID", payment.OrderId.Value);
                        }
                        else if (payment.BranchCampaignId.HasValue)
                        {
                            await ActivateBranchCampaignAsync(payment.BranchCampaignId.Value, payment.Description);
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

                    if (payment != null)
                    {
                        await _notificationPusher.PushPaymentStatusAsync(
                            payment.UserId, orderCode, actualStatus, payment.OrderId);
                    }
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

                // Use config URL if caller passed a relative path or nothing
                if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out _))
                {
                    var configUrl = _configuration["PayOS:WebhookUrl"];
                    if (string.IsNullOrWhiteSpace(configUrl))
                        throw new InvalidOperationException(
                            "WebhookUrl must be an absolute URL. Set PayOS:WebhookUrl in appsettings.");
                    webhookUrl = configUrl;
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

        public async Task<bool> HandleWebhookAsync(Webhook webhook)
        {
            try
            {
                if (webhook?.Data == null)
                {
                    _logger.LogWarning("Received invalid PayOS webhook payload");
                    return false;
                }

                WebhookData verifiedData;
                if (_isDebugMode)
                {
                    verifiedData = webhook.Data;
                }
                else
                {
                    verifiedData = await _payOS.Webhooks.VerifyAsync(webhook);
                }

                if (verifiedData.OrderCode <= 0)
                {
                    _logger.LogWarning("Webhook missing valid orderCode");
                    return false;
                }

                var payment = await _paymentRepo.GetPaymentByOrderCode(verifiedData.OrderCode);
                if (payment == null)
                {
                    _logger.LogWarning("Webhook received for unknown OrderCode={OrderCode}", verifiedData.OrderCode);
                    // Acknowledge unknown events to avoid endless retries from provider.
                    return true;
                }

                if (payment.Status != "PENDING")
                {
                    _logger.LogInformation(
                        "Webhook ignored for OrderCode={OrderCode} because status is {Status}",
                        verifiedData.OrderCode,
                        payment.Status);
                    return true;
                }

                // Reuse existing confirmation flow so side-effects are consistent
                // (order move, subscription activation, branch campaign activation).
                var provisionalStatus = webhook.Success || verifiedData.Code == "00" ? "PAID" : "CANCELLED";
                await ConfirmPaymentFromRedirect(verifiedData.OrderCode, provisionalStatus, verifiedData.Reference);

                _logger.LogInformation("Webhook processed successfully for OrderCode={OrderCode}", verifiedData.OrderCode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process PayOS webhook");
                return false;
            }
        }

                public async Task<PaymentLinkResult> CreateCampaignPaymentLink(int userId, int branchId, int branchCampaignId)
        {
            try
            {
                var branch = await _branchRepo.GetByIdAsync(branchId);
                if (branch == null)
                    return new PaymentLinkResult { Success = false, Message = "Chi nhánh không tồn tại." };

                var joinJoin = await _branchCampaignRepo.GetByIdAsync(branchCampaignId);
                if (joinJoin == null || joinJoin.BranchId != branchId)
                    return new PaymentLinkResult { Success = false, Message = "Yêu cầu tham gia không hợp lệ." };

                if (joinJoin.IsActive == true)
                    return new PaymentLinkResult { Success = false, Message = "Bạn đã thanh toán cho chiến dịch này." };

                int timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int random = new Random().Next(100, 999);
                long orderCode = long.Parse($"{timestamp}{random}");

                if (orderCode > int.MaxValue) orderCode = timestamp;
                while (await _paymentRepo.OrderCodeExists(orderCode))
                {
                    random = new Random().Next(100, 999);
                    orderCode = long.Parse($"{timestamp}{random}");
                    if (orderCode > int.MaxValue) { orderCode = timestamp; break; }
                }

                int campaignFee = CampaignJoinFee;
                var description = $"Phi tham gia campaign {branch.Name}";
                var payOsDescription = BuildPayOSDescription(description, "Phi tham gia campaign");

                var payment = await _paymentRepo.CreatePayment(
                    userId: userId,
                    orderCode: orderCode,
                    branchId: branchId,
                    amount: campaignFee,
                    description: description,
                    checkoutUrl: null,
                    orderId: null,
                    branchCampaignId: branchCampaignId
                );

                var returnUrl = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:4000/Payment/success";
                var cancelUrl = _configuration["PayOS:CancelUrl"] ?? "http://localhost:4000/Payment/cancel";
                var paymentData = new CreatePaymentLinkRequest
                {
                    OrderCode = (int)orderCode,
                    Amount = campaignFee,
                    Description = payOsDescription,
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl
                };

                string paymentLinkId, checkoutUrl, qrCode;
                if (_isDebugMode)
                {
                    paymentLinkId = "DEBUG-CAMP-" + orderCode;
                    checkoutUrl = returnUrl + "?debug=true&orderCode=" + orderCode;
                    qrCode = checkoutUrl;
                }
                else
                {
                    var paymentLinkResponse = await _payOS.PaymentRequests.CreateAsync(paymentData);
                    paymentLinkId = paymentLinkResponse.PaymentLinkId;
                    checkoutUrl = paymentLinkResponse.CheckoutUrl;
                    qrCode = paymentLinkResponse.QrCode;
                }

                await _paymentRepo.UpdatePaymentWithPayOSDetails(orderCode, "PENDING", paymentLinkId, checkoutUrl);

                return new PaymentLinkResult
                {
                    Success = true,
                    PaymentUrl = checkoutUrl,
                    QrCode = qrCode,
                    OrderCode = orderCode,
                    PaymentLinkId = paymentLinkId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campaign payment link");
                return new PaymentLinkResult { Success = false, Message = "Lỗi khi tạo link thanh toán" };
            }
        }

        public async Task<PaymentLinkResult> CreateVendorSystemCampaignPaymentLink(
            int userId,
            int campaignId,
            int vendorId,
            List<int> pendingBranchCampaignIds)
        {
            try
            {
                if (pendingBranchCampaignIds == null || pendingBranchCampaignIds.Count == 0)
                    return new PaymentLinkResult { Success = false, Message = "Không có chi nhánh cần thanh toán." };

                int campaignFee = 20000;
                // distinct while preserving input order
                var distinct = new List<int>();
                var seen = new HashSet<int>();
                foreach (var id in pendingBranchCampaignIds)
                {
                    if (id > 0 && seen.Add(id)) distinct.Add(id);
                }
                if (distinct.Count == 0)
                    return new PaymentLinkResult { Success = false, Message = "Danh sách BranchCampaignIds không hợp lệ." };

                var totalAmount = campaignFee * distinct.Count;

                // PayOS Amount is int; keep it safe
                if (totalAmount <= 0 || totalAmount > int.MaxValue)
                    return new PaymentLinkResult { Success = false, Message = "Tổng tiền thanh toán không hợp lệ." };

                var firstBranchCampaignId = distinct[0];
                var firstBranchCampaign = await _branchCampaignRepo.GetByIdAsync(firstBranchCampaignId);
                if (firstBranchCampaign == null)
                    return new PaymentLinkResult { Success = false, Message = "BranchCampaign không hợp lệ." };

                var branch = firstBranchCampaign.Branch;
                if (branch == null || !branch.VendorId.HasValue || branch.VendorId.Value != vendorId)
                    return new PaymentLinkResult { Success = false, Message = "Chi nhánh không thuộc vendor này." };

                // Ensure provided ids belong to the same (campaignId, vendorId) and are pending
                var pendingRows = await _branchCampaignRepo.GetPendingByCampaignAndVendorAsync(campaignId, vendorId);
                if (pendingRows == null || pendingRows.Count == 0)
                    return new PaymentLinkResult { Success = false, Message = "Không có chi nhánh pending cho chiến dịch này." };

                var pendingIdSet = new HashSet<int>(pendingRows.Select(r => r.Id));
                foreach (var id in distinct)
                {
                    if (!pendingIdSet.Contains(id))
                        return new PaymentLinkResult { Success = false, Message = "Danh sách chi nhánh thanh toán không hợp lệ." };
                }

                // Create one payment for the vendor's whole campaign selection
                int timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                int random = new Random().Next(100, 999);
                long orderCode = long.Parse($"{timestamp}{random}");
                if (orderCode > int.MaxValue) orderCode = timestamp;
                while (await _paymentRepo.OrderCodeExists(orderCode))
                {
                    random = new Random().Next(100, 999);
                    orderCode = long.Parse($"{timestamp}{random}");
                    if (orderCode > int.MaxValue) { orderCode = timestamp; break; }
                }

                // Keep selected ids marker in DB description for webhook activation logic.
                var description = $"Phi tham gia campaign {branch.Name} | VENDOR_SYSTEM_CAMPAIGN_BATCH:{string.Join(",", distinct)}";
                var payOsDescription = BuildPayOSDescription($"Phi tham gia campaign {branch.Name}", "Phi tham gia campaign");

                var payment = await _paymentRepo.CreatePayment(
                    userId: userId,
                    orderCode: orderCode,
                    branchId: firstBranchCampaign.BranchId,
                    amount: totalAmount,
                    description: description,
                    checkoutUrl: null,
                    orderId: null,
                    branchCampaignId: firstBranchCampaignId
                );

                var returnUrl = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:4000/Payment/success";
                var cancelUrl = _configuration["PayOS:CancelUrl"] ?? "http://localhost:4000/Payment/cancel";

                var paymentData = new CreatePaymentLinkRequest
                {
                    OrderCode = (int)orderCode,
                    Amount = totalAmount,
                    Description = payOsDescription,
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl
                };

                string paymentLinkId, checkoutUrl, qrCode;
                if (_isDebugMode)
                {
                    paymentLinkId = "DEBUG-VENDOR-CAMP-" + orderCode;
                    checkoutUrl = returnUrl + "?debug=true&orderCode=" + orderCode;
                    qrCode = checkoutUrl;
                }
                else
                {
                    var paymentLinkResponse = await _payOS.PaymentRequests.CreateAsync(paymentData);
                    paymentLinkId = paymentLinkResponse.PaymentLinkId;
                    checkoutUrl = paymentLinkResponse.CheckoutUrl;
                    qrCode = paymentLinkResponse.QrCode;
                }

                await _paymentRepo.UpdatePaymentWithPayOSDetails(orderCode, "PENDING", paymentLinkId, checkoutUrl);

                return new PaymentLinkResult
                {
                    Success = true,
                    PaymentUrl = checkoutUrl,
                    QrCode = qrCode,
                    OrderCode = orderCode,
                    PaymentLinkId = paymentLinkId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor system campaign payment link");
                return new PaymentLinkResult { Success = false, Message = "Lỗi khi tạo link thanh toán" };
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
        private async Task ActivateBranchCampaignAsync(int branchCampaignId, string? paymentDescription)
        {
            var branchCampaign = await _branchCampaignRepo.GetByIdAsync(branchCampaignId);
            if (branchCampaign == null) return;

            // Detect + parse batch ids from description.
            // Supported formats:
            // - "...VENDOR_SYSTEM_CAMPAIGN_BATCH:{id1,id2}"
            // - "Phi tham gia:{id1,id2}" (legacy format seen in production)
            var selectedIds = new List<int>();
            var desc = paymentDescription ?? string.Empty;
            try
            {
                // Prefer marker format
                var marker = "VENDOR_SYSTEM_CAMPAIGN_BATCH:";
                var idx = desc.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                string? idsPart = null;
                if (idx >= 0)
                {
                    var tail = desc.Substring(idx + marker.Length).Trim();
                    var endIdx = tail.IndexOfAny(new[] { ' ', ';', '|' });
                    idsPart = endIdx >= 0 ? tail.Substring(0, endIdx) : tail;
                }
                else
                {
                    // Fallback: take substring after last ':' (e.g. "Phi tham gia:23,24")
                    var colonIdx = desc.LastIndexOf(':');
                    if (colonIdx >= 0 && colonIdx + 1 < desc.Length)
                    {
                        idsPart = desc.Substring(colonIdx + 1).Trim();
                    }
                }

                if (!string.IsNullOrWhiteSpace(idsPart))
                {
                    foreach (var s in idsPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (int.TryParse(s, out var id) && id > 0)
                            selectedIds.Add(id);
                    }
                }
            }
            catch
            {
                // ignore parsing errors; fall back below
            }

            var isBatch = selectedIds.Count > 1 ||
                          (!string.IsNullOrWhiteSpace(paymentDescription) &&
                           paymentDescription.Contains("VENDOR_SYSTEM_CAMPAIGN_BATCH", StringComparison.OrdinalIgnoreCase));

            if (!isBatch)
            {
                branchCampaign.IsActive = true;
                await _branchCampaignRepo.UpdateAsync(branchCampaign);
                _logger.LogInformation("Activated BranchCampaign {id}", branchCampaignId);
                return;
            }

            // If parsing failed, fall back to old behavior: activate all pending rows
            // for this vendor + campaign (pre-marker payments / legacy links).
            if (selectedIds.Count == 0)
            {
                var vendorId = branchCampaign.Branch?.VendorId;
                if (vendorId.HasValue)
                {
                    var pendingRows = await _branchCampaignRepo.GetPendingByCampaignAndVendorAsync(branchCampaign.CampaignId, vendorId.Value);
                    foreach (var row in pendingRows)
                    {
                        row.IsActive = true;
                        await _branchCampaignRepo.UpdateAsync(row);
                    }

                    _logger.LogInformation(
                        "Activated {Count} pending BranchCampaigns (legacy batch) for Vendor {VendorId}, Campaign {CampaignId}",
                        pendingRows.Count, vendorId.Value, branchCampaign.CampaignId);
                    return;
                }

                // Worst-case: at least activate the anchor row
                selectedIds.Add(branchCampaignId);
            }

            var activatedCount = 0;
            foreach (var id in selectedIds.Distinct())
            {
                var row = await _branchCampaignRepo.GetByIdAsync(id);
                if (row == null) continue;
                if (row.CampaignId != branchCampaign.CampaignId) continue; // safety
                if (row.IsActive == true) continue;

                row.IsActive = true;
                await _branchCampaignRepo.UpdateAsync(row);
                activatedCount++;
            }

            _logger.LogInformation(
                "Activated {Count} BranchCampaigns (batch) for Campaign {CampaignId}",
                activatedCount, branchCampaign.CampaignId);
        }

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
            branch.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(SubscriptionDurationDays);
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

                if (!branch.VendorId.HasValue || branch.VendorId.Value <= 0)
                {
                    throw new DomainExceptions("Vendor not assigned to branch when confirming payment");
                }

                var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId.Value)
                    ?? throw new DomainExceptions("Vendor not found when confirming payment");

                // Instead of adding balance here, it will be handled when the vendor decides (OrderService)
                order.Status = OrderStatus.AwaitingVendorConfirmation;
                await _orderRepository.Update(order);

                if (branch.ManagerId.HasValue)
                {
                    await _notificationPusher.PushToUserAsync(branch.ManagerId.Value, new NotificationDto
                    {
                        Type = NotificationType.OrderStatusUpdate.ToString(),
                        Title = "Đơn hàng mới",
                        Message = $"Có đơn hàng #{orderId} vừa thanh toán, cần xác nhận.",
                        ReferenceId = orderId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
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
                QrCode = payment.CheckoutUrl,
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

        private static string BuildPayOSDescription(string? raw, string fallback)
        {
            var normalized = string.IsNullOrWhiteSpace(raw)
                ? fallback
                : string.Join(" ", raw.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            const int maxLen = 25;
            if (normalized.Length <= maxLen)
            {
                return normalized;
            }

            return normalized[..maxLen];
        }
    }
}


