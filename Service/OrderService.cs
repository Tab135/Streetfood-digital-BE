using BO.Common;
using BO.DTO.Order;
using BO.Entities;
using BO.Enums;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using Service.PaymentsService;
using Service.Utils;
using System.Security.Cryptography;
using System.Threading;

namespace Service;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IDishRepository _dishRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly IVoucherRepository _voucherRepository;
    private readonly IUserVoucherRepository _userVoucherRepository;
    private readonly IBranchCampaignRepository _branchCampaignRepository;
    private readonly INotificationService _notificationService;
    private readonly IQuestProgressService _questProgressService;
    private readonly ISettingService _settingService;
    private readonly IUserService _userService;
    private readonly IPaymentService _paymentService;
    private static readonly TimeSpan PendingCheckoutAbandonmentThreshold = TimeSpan.FromMinutes(10);

    public OrderService(
        IOrderRepository orderRepository,
        IBranchRepository branchRepository,
        IDishRepository dishRepository,
        IUserRepository userRepository,
        IVendorRepository vendorRepository,
        IVoucherRepository voucherRepository,
        IUserVoucherRepository userVoucherRepository,
        IBranchCampaignRepository branchCampaignRepository,
        INotificationService notificationService,
        IQuestProgressService questProgressService,
        ISettingService settingService,
        IUserService userService,
        IPaymentService paymentService
    )
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
        _voucherRepository = voucherRepository ?? throw new ArgumentNullException(nameof(voucherRepository));
        _userVoucherRepository = userVoucherRepository ?? throw new ArgumentNullException(nameof(userVoucherRepository));
        _branchCampaignRepository = branchCampaignRepository ?? throw new ArgumentNullException(nameof(branchCampaignRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _questProgressService = questProgressService ?? throw new ArgumentNullException(nameof(questProgressService));
        _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequest request, int userId)
    {
        await EnsureUserExistsAsync(userId);
        var branch = await _branchRepository.GetByIdAsync(request.BranchId)
            ?? throw new DomainExceptions("Branch not found");

        if (!branch.IsSubscribed)
        {
            throw new DomainExceptions("This branch is not subscribed and cannot accept order checkout.");
        }

        var (orderDishes, totalAmount) = await BuildValidatedOrderDishesAsync(request.BranchId, request.Items);
        await ValidateOrderVoucherAsync(request.AppliedVoucherId, userId, branch);

        var discountAmount = request.DiscountAmount ?? 0m;
        if (discountAmount < 0)
        {
            throw new DomainExceptions("Discount amount must be non-negative");
        }

        var finalAmount = totalAmount - discountAmount;
        if (finalAmount < 0)
        {
            throw new DomainExceptions("Final amount cannot be negative");
        }

        var order = new Order
        {
            UserId = userId,
            BranchId = branch.BranchId,
            AppliedVoucherId = request.AppliedVoucherId,
            Status = OrderStatus.Pending,
            Table = request.Table,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note,
            TotalAmount = totalAmount,
            DiscountAmount = request.DiscountAmount,
            FinalAmount = finalAmount,
            IsTakeAway = request.IsTakeAway,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _orderRepository.Create(order, orderDishes);
        return MapToDto(created);
    }

    public async Task<(OrderResponseDto order, bool createdNew, int? previousAppliedVoucherId)> CreateOrUpdatePendingOrderForCartAsync(CreateOrderRequest request, int userId)
    {
        await EnsureUserExistsAsync(userId);
        var branch = await _branchRepository.GetByIdAsync(request.BranchId)
            ?? throw new DomainExceptions("Branch not found");

        if (!branch.IsSubscribed)
        {
            throw new DomainExceptions("This branch is not subscribed and cannot accept order checkout.");
        }

        var (orderDishes, totalAmount) = await BuildValidatedOrderDishesAsync(request.BranchId, request.Items);
        await ValidateOrderVoucherAsync(request.AppliedVoucherId, userId, branch);

        var discountAmount = request.DiscountAmount ?? 0m;
        if (discountAmount < 0)
        {
            throw new DomainExceptions("Discount amount must be non-negative");
        }

        var finalAmount = totalAmount - discountAmount;
        if (finalAmount < 0)
        {
            throw new DomainExceptions("Final amount cannot be negative");
        }

        var existingPendingOrder = await _orderRepository.GetLatestPendingByUserAndBranch(userId, branch.BranchId);
        var staleBeforeUtc = DateTime.UtcNow.Subtract(PendingCheckoutAbandonmentThreshold);

        if (existingPendingOrder != null)
        {
            var autoCancelled = await TryCancelPendingOrderForAbandonmentAsync(existingPendingOrder, staleBeforeUtc);
            if (autoCancelled)
            {
                existingPendingOrder = null;
            }
        }

        if (existingPendingOrder != null)
        {
            var previousAppliedVoucherId = existingPendingOrder.AppliedVoucherId;

            existingPendingOrder.AppliedVoucherId = request.AppliedVoucherId;
            existingPendingOrder.Table = request.Table;
            existingPendingOrder.PaymentMethod = request.PaymentMethod;
            existingPendingOrder.Note = request.Note;
            existingPendingOrder.TotalAmount = totalAmount;
            existingPendingOrder.DiscountAmount = request.DiscountAmount;
            existingPendingOrder.FinalAmount = finalAmount;
            existingPendingOrder.IsTakeAway = request.IsTakeAway;

            var updated = await _orderRepository.Update(existingPendingOrder, orderDishes);
            return (MapToDto(updated), false, previousAppliedVoucherId);
        }

        var order = new Order
        {
            UserId = userId,
            BranchId = branch.BranchId,
            AppliedVoucherId = request.AppliedVoucherId,
            Status = OrderStatus.Pending,
            Table = request.Table,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note,
            TotalAmount = totalAmount,
            DiscountAmount = request.DiscountAmount,
            FinalAmount = finalAmount,
            IsTakeAway = request.IsTakeAway,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _orderRepository.Create(order, orderDishes);
        return (MapToDto(created), true, null);
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId)
    {
        var order = await _orderRepository.GetById(orderId);
        if (order == null)
        {
            return null;
        }


        return MapToDto(order);
    }

    public async Task<PaginatedResponse<OrderResponseDto>> GetMyOrdersAsync(int userId, int pageNumber, int pageSize, OrderStatus? status = null)
    {
        await EnsureUserExistsAsync(userId);

        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        var effectiveStatuses = status.HasValue
            ? new List<OrderStatus> { status.Value }
            : new List<OrderStatus>
            {
                OrderStatus.Pending,
                OrderStatus.AwaitingVendorConfirmation,
                OrderStatus.Paid,
                OrderStatus.Cancelled,
                OrderStatus.Complete
            };

        var (orders, totalCount) = await _orderRepository.GetByUserId(userId, pageNumber, pageSize, effectiveStatuses);
        var items = orders.Select(MapToDto).ToList();

        return new PaginatedResponse<OrderResponseDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResponse<OrderResponseDto>> GetVendorOrdersAsync(int vendorUserId, int pageNumber, int pageSize, OrderStatus? status = null)
    {
        var vendor = await _vendorRepository.GetByUserIdAsync(vendorUserId)
            ?? throw new DomainExceptions("Vendor not found");

        var branches = await _branchRepository.GetAllByVendorIdAsync(vendor.VendorId);
        if (branches.Count == 0)
        {
            return new PaginatedResponse<OrderResponseDto>(new List<OrderResponseDto>(), 0, pageNumber, pageSize);
        }

        var branchIds = branches.Select(b => b.BranchId).ToList();

        var effectiveStatuses = status.HasValue
            ? new List<OrderStatus> { status.Value }
            : new List<OrderStatus>
            {
                OrderStatus.Pending,
                OrderStatus.AwaitingVendorConfirmation,
                OrderStatus.Paid,
                OrderStatus.Cancelled,
                OrderStatus.Complete
            };
        var (orders, totalCount) = await _orderRepository.GetByBranchIds(branchIds, pageNumber, pageSize, effectiveStatuses);
        var items = orders.Select(MapToDto).ToList();

        return new PaginatedResponse<OrderResponseDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResponse<OrderResponseDto>> GetVendorOrdersByBranchAsync(int vendorUserId, int branchId, int pageNumber, int pageSize, OrderStatus? status = null)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId)
            ?? throw new DomainExceptions("Branch not found");

        var isBranchManager = branch.ManagerId.HasValue && branch.ManagerId.Value == vendorUserId;

        if (!isBranchManager)
        {
            var vendor = await _vendorRepository.GetByUserIdAsync(vendorUserId)
                ?? throw new DomainExceptions("Vendor not found");

            if (branch.VendorId != vendor.VendorId)
            {
                throw new DomainExceptions("You do not have access to this branch", "ERR_FORBIDDEN");
            }
        }

        if (status == OrderStatus.Pending)
        {
            throw new DomainExceptions("Pending orders are not visible to vendors before payment");
        }

        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        var effectiveStatuses = status.HasValue
            ? new List<OrderStatus> { status.Value }
            : new List<OrderStatus>
            {
                OrderStatus.AwaitingVendorConfirmation,
                OrderStatus.Paid,
                OrderStatus.Cancelled,
                OrderStatus.Complete
            };

        var (orders, totalCount) = await _orderRepository.GetByBranchIds(new List<int> { branchId }, pageNumber, pageSize, effectiveStatuses);
        var items = orders.Select(MapToDto).ToList();

        return new PaginatedResponse<OrderResponseDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResponse<OrderResponseDto>> GetManagerOrdersAsync(int managerUserId, int pageNumber, int pageSize, OrderStatus? status = null)
    {
        var branches = await _branchRepository.GetAllByManagerIdAsync(managerUserId);
        if (branches.Count == 0)
        {
            throw new DomainExceptions("No branch assigned to this manager", "ERR_NOT_FOUND");
        }

        var branchIds = branches.Select(b => b.BranchId).ToList();

        if (status == OrderStatus.Pending)
        {
            throw new DomainExceptions("Pending orders are not visible to managers before payment");
        }

        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        var effectiveStatuses = status.HasValue
            ? new List<OrderStatus> { status.Value }
            : new List<OrderStatus>
            {
                OrderStatus.AwaitingVendorConfirmation,
                OrderStatus.Paid,
                OrderStatus.Cancelled,
                OrderStatus.Complete
            };

        var (orders, totalCount) = await _orderRepository.GetByBranchIds(branchIds, pageNumber, pageSize, effectiveStatuses);
        var items = orders.Select(MapToDto).ToList();

        return new PaginatedResponse<OrderResponseDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<OrderPickupCodeResponseDto> GetOrderPickupCodeAsync(int orderId, int userId)
    {
        var order = await _orderRepository.GetById(orderId)
            ?? throw new DomainExceptions("Đơn hàng không tồn tại");

        EnsureOrderOwnership(order, userId);

        if (order.Status != OrderStatus.Paid)
        {
            throw new DomainExceptions("Mã lấy hàng chỉ có sẵn khi đơn hàng đã sẵn sàng để lấy");
        }

        if (string.IsNullOrWhiteSpace(order.CompletionCode))
        {
            throw new DomainExceptions("Mã lấy hàng chưa được tạo");
        }

        return new OrderPickupCodeResponseDto
        {
            OrderId = order.OrderId,
            VerificationCode = order.CompletionCode,
            QrContent = $"SF|ORDER:{order.OrderId}|CODE:{order.CompletionCode}"
        };
    }

    public async Task<OrderResponseDto> CancelOrderAsync(int orderId, int userId)
    {
        var order = await _orderRepository.GetById(orderId)
            ?? throw new DomainExceptions("Đơn hàng không tồn tại", "ERR_NOT_FOUND");

        EnsureOrderOwnership(order, userId);

        if (order.Status != OrderStatus.Pending)
        {
            throw new DomainExceptions("Đơn hàng chỉ có thể bị hủy khi đang chờ thanh toán", "ERR_BAD_REQUEST");
        }

        await _paymentService.CancelOrderPaymentAsync(orderId);
        await RestoreVoucherUsageForCancellationAsync(order);

        order.Status = OrderStatus.Cancelled;
        order.CompletionCode = null;

        var updated = await _orderRepository.Update(order);
        return MapToDto(updated);
    }

    public async Task<OrderResponseDto> UpdateOrderAsync(int orderId, UpdateOrderRequest request, int userId)
    {
        var order = await _orderRepository.GetById(orderId)
            ?? throw new DomainExceptions("Đơn hàng không tồn tại");

        var branch = await _branchRepository.GetByIdAsync(order.BranchId)
            ?? throw new DomainExceptions("Chi nhánh không tồn tại");

        if (!branch.ManagerId.HasValue || branch.ManagerId.Value != userId )
        {
            throw new DomainExceptions("Bạn không quản lý chi nhánh này", "ERR_FORBIDDEN");
        }

        if (order.Status == OrderStatus.Complete)
        {
            var hasRestrictedFields = request.Status.HasValue
                || request.PaymentMethod != null
                || request.Note != null
                || request.DiscountAmount.HasValue
                || request.IsTakeAway.HasValue
                || request.Items != null;

            if (hasRestrictedFields)
            {
                throw new DomainExceptions("Chỉ có số bàn có thể được cập nhật sau khi đơn hàng hoàn thành");
            }

            if (string.IsNullOrWhiteSpace(request.Table))
            {
                throw new DomainExceptions("Số bàn là bắt buộc khi cập nhật đơn hàng đã hoàn thành");
            }

            order.Table = request.Table.Trim();
            var completedOrder = await _orderRepository.Update(order);
            return MapToDto(completedOrder);
        }

        if (order.Status == OrderStatus.Complete)
        {
            throw new DomainExceptions("Đơn hàng không thể được cập nhật sau khi thanh toán đã hoàn tất");
        }

        if (request.Status.HasValue)
        {
            if (request.Status.Value != OrderStatus.Cancelled)
            {
                throw new DomainExceptions("Chỉ cho phép hủy đơn hàng từ cập nhật của người dùng");
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new DomainExceptions("Chỉ có đơn hàng đang chờ có thể được hủy bởi người dùng");
            }

            await RestoreVoucherUsageForCancellationAsync(order);

            order.Status = OrderStatus.Cancelled;
            order.CompletionCode = null;
        }

        if (request.Table != null)
        {
            order.Table = request.Table;
        }

        if (request.PaymentMethod != null)
        {
            order.PaymentMethod = request.PaymentMethod;
        }

        if (request.Note != null)
        {
            order.Note = request.Note;
        }

        if (request.IsTakeAway.HasValue)
        {
            order.IsTakeAway = request.IsTakeAway.Value;
        }

        if (request.DiscountAmount.HasValue)
        {
            if (request.DiscountAmount.Value < 0)
            {
                throw new DomainExceptions("Số tiền giảm giá phải là số dương");
            }

            order.DiscountAmount = request.DiscountAmount;
        }

        List<OrderDish>? orderDishes = null;
        if (request.Items != null)
        {
            var recalculated = await BuildValidatedOrderDishesAsync(order.BranchId, request.Items);
            orderDishes = recalculated.orderDishes;
            order.TotalAmount = recalculated.totalAmount;
        }

        var discount = order.DiscountAmount ?? 0m;
        order.FinalAmount = order.TotalAmount - discount;
        if (order.FinalAmount < 0)
        {
            throw new DomainExceptions("Số tiền cuối cùng không thể là số âm");
        }

        var updated = await _orderRepository.Update(order, orderDishes);
        return MapToDto(updated);
    }

    public async Task<OrderResponseDto> VendorDecideOrderAsync(int orderId, int vendorUserId, bool approve)
    {
        var order = await _orderRepository.GetById(orderId)
            ?? throw new DomainExceptions("Đơn hàng không tồn tại");

        var branch = await _branchRepository.GetByIdAsync(order.BranchId)
            ?? throw new DomainExceptions("Chi nhánh không tồn tại");

        if (!branch.VendorId.HasValue || branch.VendorId.Value <= 0)
        {
            throw new DomainExceptions("Chủ quán không được gán cho chi nhánh này", "ERR_NOT_FOUND");
        }

        var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId.Value)
            ?? throw new DomainExceptions("Chủ quán không tồn tại", "ERR_NOT_FOUND");

        var isBranchManager = branch.ManagerId.HasValue && branch.ManagerId.Value == vendorUserId;
        var isVendorOwner = vendor.UserId == vendorUserId;

        if (!isVendorOwner && !isBranchManager)
        {
            throw new DomainExceptions("Bạn không sở hữu chi nhánh này", "ERR_FORBIDDEN");
        }

        if (order.Status != OrderStatus.AwaitingVendorConfirmation)
        {
            throw new DomainExceptions("Đơn hàng không đang chờ xác nhận của chủ quán");
        }

        if (approve)
        {
            order.Status = OrderStatus.Paid;
            order.CompletionCode = GenerateCompletionCode();
        }
        else
        {
            order.Status = OrderStatus.Cancelled;
            order.CompletionCode = null;
            var user = await _userRepository.GetUserById(order.UserId)
                ?? throw new DomainExceptions("Người dùng không tồn tại khi hoàn tiền", "ERR_NOT_FOUND");
            
            user.MoneyBalance += order.FinalAmount;
            await _userRepository.UpdateAsync(user);
            await RestoreVoucherAfterVendorRejectAsync(order);
        }

        var updated = await _orderRepository.Update(order);

        // Notify customer about order status change
        var statusText = approve ? "được duyệt" : "đã bị hủy";
        var title = approve ? "Đơn hàng được duyệt" : "Đơn hàng đã bị hủy";
        var message = $"Đơn hàng #{order.OrderId} ở {branch.Name} đã {statusText}";
        var pushData = new
        {
            type = "order_status",
            orderId = order.OrderId,
            branchName = branch.Name,
            orderStatus = statusText,
        };

        await _notificationService.NotifyAsync(
            order.UserId,
            NotificationType.OrderStatusUpdate,
            title,
            message,
            order.OrderId,
            pushData);

        return MapToDto(updated);
    }

    public async Task<OrderResponseDto> VendorCompleteOrderAsync(int orderId, int vendorUserId, string verificationCode)
    {
        var order = await _orderRepository.GetById(orderId)
            ?? throw new DomainExceptions("Đơn hàng không tồn tại", "ERR_NOT_FOUND");

        var branch = await _branchRepository.GetByIdAsync(order.BranchId)
            ?? throw new DomainExceptions("Chi nhánh không tồn tại", "ERR_NOT_FOUND");

        if (!branch.VendorId.HasValue || branch.VendorId.Value <= 0)
        {
            throw new DomainExceptions("Chủ quán không được gán cho chi nhánh này", "ERR_NOT_FOUND");
        }

        var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId.Value)
            ?? throw new DomainExceptions("Chủ quán không tồn tại", "ERR_NOT_FOUND");

        var isBranchManager = branch.ManagerId.HasValue && branch.ManagerId.Value == vendorUserId;
        var isVendorOwner = vendor.UserId == vendorUserId;

        if (!isVendorOwner && !isBranchManager)
        {
            throw new DomainExceptions("Bạn không sở hữu chi nhánh này", "ERR_FORBIDDEN");
        }

        if (order.Status != OrderStatus.Paid)
        {
            throw new DomainExceptions("Đơn hàng phải được thanh toán trước khi có thể hoàn thành", "ERR_BAD_REQUEST");
        }

        if (string.IsNullOrWhiteSpace(verificationCode))
        {
            throw new DomainExceptions("Mã xác minh là bắt buộc", "ERR_BAD_REQUEST");
        }

        if (string.IsNullOrWhiteSpace(order.CompletionCode))
        {
            throw new DomainExceptions("Mã xác minh đơn hàng bị thiếu", "ERR_BAD_REQUEST");
        }

        var normalizedCode = verificationCode.Trim();
        if (!string.Equals(order.CompletionCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainExceptions("Mã xác minh không hợp lệ", "ERR_BAD_REQUEST");
        }

        order.Status = OrderStatus.Complete;
        order.CompletionCode = null;

        // Give XP to user for completing the order
        var orderXP = _settingService.GetInt("orderXP", 0);
        if (orderXP > 0)
        {
            await _userService.AddXPAsync(order.UserId, orderXP);
            order.OrderXP = orderXP;
        }

        var vendorSettlementAmount = await CalculateVendorSettlementAmountAsync(order);
        vendor.MoneyBalance += vendorSettlementAmount;
        await _vendorRepository.UpdateAsync(vendor);

        var updated = await _orderRepository.Update(order);

        // Notify customer about order completion
        var pushData = new
        {
            type = "order_status",
            orderId = order.OrderId,
            branchName = branch.Name,
            orderStatus = "complete",
        };

        await _notificationService.NotifyAsync(
            order.UserId,
            NotificationType.OrderStatusUpdate,
            "Đơn hàng đã hoàn thành",
            $"Đơn hàng #{order.OrderId} ở {branch.Name} đã được hoàn thành",
            order.OrderId,
            pushData);

        await _questProgressService.UpdateProgressAsync(order.UserId, QuestTaskType.ORDER_AMOUNT, (int)order.FinalAmount);

        return MapToDto(updated);
    }

    public async Task<bool> DeleteOrderAsync(int orderId, int userId)
    {
        var order = await _orderRepository.GetById(orderId)
            ?? throw new DomainExceptions("Đơn hàng không tồn tại", "ERR_NOT_FOUND");

        EnsureOrderOwnership(order, userId);

        if (order.Status != OrderStatus.Pending)
        {
            throw new DomainExceptions("Đơn hàng không thể bị xóa sau khi thanh toán đã hoàn tất", "ERR_BAD_REQUEST");
        }

        await _orderRepository.Delete(orderId);
        return true;
    }

    public async Task<int> CancelAbandonedPendingOrdersAsync(TimeSpan inactivityTimeout, CancellationToken cancellationToken = default)
    {
        if (inactivityTimeout <= TimeSpan.Zero)
        {
            throw new DomainExceptions("Thời gian chờ không hợp lệ", "ERR_BAD_REQUEST");
        }

        var staleBeforeUtc = DateTime.UtcNow.Subtract(inactivityTimeout);
        var stalePendingOrders = await _orderRepository.GetPendingOrdersNotUpdatedSince(staleBeforeUtc);

        var cancelledCount = 0;

        foreach (var staleOrder in stalePendingOrders)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var currentOrder = await _orderRepository.GetById(staleOrder.OrderId);
            if (currentOrder == null)
            {
                continue;
            }

            var cancelled = await TryCancelPendingOrderForAbandonmentAsync(currentOrder, staleBeforeUtc);
            if (cancelled)
            {
                cancelledCount++;
            }
        }

        return cancelledCount;
    }

    private async Task<(List<OrderDish> orderDishes, decimal totalAmount)> BuildValidatedOrderDishesAsync(int branchId, List<CreateOrderDishRequest> items)
    {
        if (items == null || items.Count == 0)
        {
            throw new DomainExceptions("Ít nhất một món ăn là bắt buộc", "ERR_BAD_REQUEST");
        }

        var normalizedItems = items
            .GroupBy(i => i.DishId)
            .Select(g => new CreateOrderDishRequest
            {
                DishId = g.Key,
                Quantity = g.Sum(x => x.Quantity)
            })
            .ToList();

        var orderDishes = new List<OrderDish>();
        decimal totalAmount = 0m;

        foreach (var item in normalizedItems)
        {
            if (item.Quantity <= 0)
            {
                throw new DomainExceptions("Số lượng phải ít nhất là 1", "ERR_BAD_REQUEST");
            }

            var branchDish = await _dishRepository.GetBranchDishAsync(branchId, item.DishId);
            if (branchDish == null)
            {
                throw new DomainExceptions($"Món ăn {item.DishId} không có trong chi nhánh này", "ERR_BAD_REQUEST");
            }

            if (branchDish.IsSoldOut)
            {
                throw new DomainExceptions($"Món ăn {item.DishId} hiện tại đã hết", "ERR_BAD_REQUEST");
            }

            var dish = await _dishRepository.GetByIdAsync(item.DishId)
                ?? throw new DomainExceptions($"Món ăn {item.DishId} không tồn tại", "ERR_NOT_FOUND");

            totalAmount += dish.Price * item.Quantity;

            orderDishes.Add(new OrderDish
            {
                DishId = item.DishId,
                BranchId = branchId,
                DishName = dish.Name,
                Price = dish.Price,
                ImageUrl = dish.ImageUrl,
                Quantity = item.Quantity,
                CreatedAt = DateTime.UtcNow
            });
        }

        return (orderDishes, totalAmount);
    }

    private static void EnsureOrderOwnership(Order order, int userId)
    {
        if (order.UserId != userId)
        {
            throw new DomainExceptions("Bạn không sở hữu đơn hàng này", "ERR_FORBIDDEN");
        }
    }

    private async Task<bool> TryCancelPendingOrderForAbandonmentAsync(Order order, DateTime staleBeforeUtc)
    {
        if (order.Status != OrderStatus.Pending || order.UpdatedAt > staleBeforeUtc)
        {
            return false;
        }

        await RestoreVoucherUsageForCancellationAsync(order);

        order.Status = OrderStatus.Cancelled;
        order.CompletionCode = null;
        await _orderRepository.Update(order);

        return true;
    }

    private async Task RestoreVoucherUsageForCancellationAsync(Order order)
    {
        if (!order.AppliedVoucherId.HasValue)
        {
            return;
        }

        var voucher = await _voucherRepository.GetByIdAsync(order.AppliedVoucherId.Value)
            ?? throw new DomainExceptions("Phiếu giảm giá không tồn tại khi hoàn tiền", "ERR_NOT_FOUND");

        if (IsSystemFundedVoucher(voucher))
        {
            var userVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(order.UserId, voucher.VoucherId)
                ?? throw new DomainExceptions("Phiếu giảm giá đã claimed không tìm thấy cho người dùng này");

            userVoucher.Quantity += 1;
            userVoucher.IsAvailable = true;
            await _userVoucherRepository.UpdateAsync(userVoucher);
            return;
        }

        if (voucher.UsedQuantity <= 0)
        {
            return;
        }

        voucher.UsedQuantity -= 1;
        await _voucherRepository.UpdateAsync(voucher);
    }

    private async Task ValidateOrderVoucherAsync(int? appliedVoucherId, int userId, Branch branch)
    {
        if (!appliedVoucherId.HasValue)
        {
            return;
        }

        var voucher = await _voucherRepository.GetByIdAsync(appliedVoucherId.Value)
            ?? throw new DomainExceptions("Phiếu giảm giá không tồn tại", "ERR_NOT_FOUND");

        if (!voucher.IsActive)
        {
            throw new DomainExceptions("Phiếu giảm giá không hoạt động", "ERR_BAD_REQUEST");
        }

        var now = DateTime.UtcNow;
        VoucherRules.EnsureVoucherIsWithinValidDateRange(voucher, now);

        if (voucher.VendorCampaignId.HasValue)
        {
            var campaign = voucher.VendorCampaign;
            if (campaign == null)
            {
                throw new DomainExceptions("Chiến dịch của phiếu giảm giá không tồn tại", "ERR_NOT_FOUND");
            }

            var joinInfo = await _branchCampaignRepository.GetByBranchAndCampaignAsync(branch.BranchId, campaign.CampaignId);
            if (joinInfo == null || joinInfo.IsActive != true)
            {
                if (campaign.CreatedByVendorId.HasValue)
                {
                    throw new DomainExceptions("Chi nhánh này chưa tham gia chiến dịch của chủ quán, không thể sử dụng phiếu giảm giá này", "ERR_BAD_REQUEST");
                }

                throw new DomainExceptions("Chi nhánh này chưa hoàn thành thanh toán tham gia chiến dịch", "ERR_BAD_REQUEST");
            }
        }

        if (IsSystemFundedVoucher(voucher))
        {
            var userVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, voucher.VoucherId)
                ?? throw new DomainExceptions("Bạn chưa nhận phiếu giảm giá này", "ERR_NOT_FOUND");

            if (!userVoucher.IsAvailable || userVoucher.Quantity <= 0)
            {
                throw new DomainExceptions("Phiếu giảm giá đã được sử dụng hoặc không khả dụng", "ERR_BAD_REQUEST");
            }

            return;
        }

        if (VoucherRules.IsOutOfStock(voucher))
        {
            throw new DomainExceptions("Phiếu giảm giá đã hết hàng", "ERR_BAD_REQUEST");
        }
    }

    private async Task EnsureUserExistsAsync(int userId)
    {
        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            throw new DomainExceptions("người dùng không tồn tại", "ERR_NOT_FOUND");
        }
    }

    private async Task<decimal> CalculateVendorSettlementAmountAsync(Order order)
    {
        if (!order.AppliedVoucherId.HasValue)
        {
            return order.FinalAmount;
        }

        var voucher = await _voucherRepository.GetByIdAsync(order.AppliedVoucherId.Value);
        if (voucher == null)
        {
            return order.FinalAmount;
        }

        // System-funded vouchers compensate vendor for the discounted part.
        // Vendor/branch-funded vouchers do not.
        if (IsSystemFundedVoucher(voucher))
        {
            return order.TotalAmount;
        }

        return order.FinalAmount;
    }

    private static bool IsSystemFundedVoucher(Voucher voucher)
    {
        if (!voucher.VendorCampaignId.HasValue)
        {
            return true;
        }

        var campaign = voucher.VendorCampaign;
        if (campaign == null)
        {
            return false;
        }

        return !campaign.CreatedByVendorId.HasValue;
    }

    private async Task RestoreVoucherAfterVendorRejectAsync(Order order)
    {
        await RestoreVoucherUsageForCancellationAsync(order);
    }

    private static string GenerateCompletionCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static OrderResponseDto MapToDto(Order order)
    {
        return new OrderResponseDto
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            UserName = order.User?.UserName ?? string.Empty,
            BranchId = order.BranchId,
            BranchName = order.Branch?.Name ?? string.Empty,
            Status = order.Status,
            Table = order.Table,
            PaymentMethod = order.PaymentMethod,
            Note = order.Note,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            FinalAmount = order.FinalAmount,
            AppliedVoucherId = order.AppliedVoucherId,
            AppliedVoucherCode = order.AppliedVoucher?.VoucherCode,
            AppliedVoucherName = order.AppliedVoucher?.Name,
            IsTakeAway = order.IsTakeAway,
            CreatedAt = order.CreatedAt,
            OrderXP = order.OrderXP,
            UpdatedAt = order.UpdatedAt,
            Items = order.OrderDishes.Select(od => new OrderDishResponseDto
            {
                DishId = od.DishId,
                DishName = !string.IsNullOrEmpty(od.DishName) ? od.DishName : (od.BranchDish?.Dish?.Name ?? string.Empty),
                Price = od.Price > 0 ? od.Price : (od.BranchDish?.Dish?.Price ?? 0m),
                Quantity = od.Quantity,
                ImageUrl = od.ImageUrl ?? od.BranchDish?.Dish?.ImageUrl
            }).ToList()
        };
    }
}
