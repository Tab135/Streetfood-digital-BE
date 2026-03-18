using BO.Common;
using BO.DTO.Order;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IDishRepository _dishRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVendorRepository _vendorRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IBranchRepository branchRepository,
        IDishRepository dishRepository,
        IUserRepository userRepository,
        IVendorRepository vendorRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _vendorRepository = vendorRepository ?? throw new ArgumentNullException(nameof(vendorRepository));
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequest request, int userId)
    {
        await EnsureUserExistsAsync(userId);
        var branch = await _branchRepository.GetByIdAsync(request.BranchId)
            ?? throw new DomainExceptions("Branch not found");

        var (orderDishes, totalAmount) = await BuildValidatedOrderDishesAsync(request.BranchId, request.Items);
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
            Status = OrderStatus.Pending,
            Table = request.Table,
            PaymentMethod = request.PaymentMethod,
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

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId)
    {
        var order = await _orderRepository.GetById(orderId);
        if (order == null)
        {
            return null;
        }

        EnsureOrderOwnership(order, userId);
        return MapToDto(order);
    }

    public async Task<PaginatedResponse<OrderResponseDto>> GetMyOrdersAsync(int userId, int pageNumber, int pageSize)
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

        var (orders, totalCount) = await _orderRepository.GetByUserId(userId, pageNumber, pageSize);
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

        if (status == OrderStatus.Pending)
        {
            throw new DomainExceptions("Pending orders are not visible to vendors before payment");
        }

        var effectiveStatus = status ?? OrderStatus.AwaitingVendorConfirmation;
        var (orders, totalCount) = await _orderRepository.GetByBranchIds(branchIds, pageNumber, pageSize, effectiveStatus);
        var items = orders.Select(MapToDto).ToList();

        return new PaginatedResponse<OrderResponseDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<OrderResponseDto> UpdateOrderAsync(int orderId, UpdateOrderRequest request, int userId)
    {
        var order = await _orderRepository.GetById(orderId)
            ?? throw new DomainExceptions("Order not found");

        EnsureOrderOwnership(order, userId);

        if (order.Status != OrderStatus.Pending)
        {
            throw new DomainExceptions("Order cannot be updated after payment is completed");
        }

        if (request.Status.HasValue)
        {
            if (request.Status.Value != OrderStatus.Cancelled)
            {
                throw new DomainExceptions("Only cancellation is allowed from user update");
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new DomainExceptions("Only pending orders can be cancelled by user");
            }

            order.Status = OrderStatus.Cancelled;
        }

        if (request.Table != null)
        {
            order.Table = request.Table;
        }

        if (request.PaymentMethod != null)
        {
            order.PaymentMethod = request.PaymentMethod;
        }

        if (request.IsTakeAway.HasValue)
        {
            order.IsTakeAway = request.IsTakeAway.Value;
        }

        if (request.LockedAt.HasValue)
        {
            order.LockedAt = request.LockedAt;
        }

        if (request.DiscountAmount.HasValue)
        {
            if (request.DiscountAmount.Value < 0)
            {
                throw new DomainExceptions("Discount amount must be non-negative");
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
            throw new DomainExceptions("Final amount cannot be negative");
        }

        var updated = await _orderRepository.Update(order, orderDishes);
        return MapToDto(updated);
    }

    public async Task<OrderResponseDto> VendorDecideOrderAsync(int orderId, int vendorUserId, bool approve)
    {
        var order = await _orderRepository.GetById(orderId)
            ?? throw new DomainExceptions("Order not found");

        var branch = await _branchRepository.GetByIdAsync(order.BranchId)
            ?? throw new DomainExceptions("Branch not found");

        var vendor = await _vendorRepository.GetByIdAsync(branch.VendorId)
            ?? throw new DomainExceptions("Vendor not found");

        if (vendor.UserId != vendorUserId)
        {
            throw new DomainExceptions("You do not own this branch", "ERR_FORBIDDEN");
        }

        if (order.Status != OrderStatus.AwaitingVendorConfirmation)
        {
            throw new DomainExceptions("Order is not waiting for vendor confirmation");
        }

        order.Status = approve ? OrderStatus.Paid : OrderStatus.Cancelled;
        var updated = await _orderRepository.Update(order);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteOrderAsync(int orderId, int userId)
    {
        var order = await _orderRepository.GetById(orderId)
            ?? throw new DomainExceptions("Order not found");

        EnsureOrderOwnership(order, userId);

        if (order.Status != OrderStatus.Pending)
        {
            throw new DomainExceptions("Order cannot be deleted after payment is completed");
        }

        await _orderRepository.Delete(orderId);
        return true;
    }

    private async Task<(List<OrderDish> orderDishes, decimal totalAmount)> BuildValidatedOrderDishesAsync(int branchId, List<CreateOrderDishRequest> items)
    {
        if (items == null || items.Count == 0)
        {
            throw new DomainExceptions("At least one dish is required");
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
                throw new DomainExceptions("Quantity must be at least 1");
            }

            var branchDish = await _dishRepository.GetBranchDishAsync(branchId, item.DishId);
            if (branchDish == null)
            {
                throw new DomainExceptions($"Dish {item.DishId} is not available in this branch");
            }

            if (branchDish.IsSoldOut)
            {
                throw new DomainExceptions($"Dish {item.DishId} is currently sold out");
            }

            var dish = await _dishRepository.GetByIdAsync(item.DishId)
                ?? throw new DomainExceptions($"Dish {item.DishId} not found");

            totalAmount += dish.Price * item.Quantity;

            orderDishes.Add(new OrderDish
            {
                DishId = item.DishId,
                BranchId = branchId,
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
            throw new DomainExceptions("You do not own this order", "ERR_FORBIDDEN");
        }
    }

    private async Task EnsureUserExistsAsync(int userId)
    {
        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            throw new DomainExceptions("User not found");
        }
    }

    private static OrderResponseDto MapToDto(Order order)
    {
        return new OrderResponseDto
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            BranchId = order.BranchId,
            BranchName = order.Branch?.Name ?? string.Empty,
            Status = order.Status,
            Table = order.Table,
            PaymentMethod = order.PaymentMethod,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            FinalAmount = order.FinalAmount,
            IsTakeAway = order.IsTakeAway,
            LockedAt = order.LockedAt,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.OrderDishes.Select(od => new OrderDishResponseDto
            {
                DishId = od.DishId,
                DishName = od.BranchDish?.Dish?.Name ?? string.Empty,
                Quantity = od.Quantity
            }).ToList()
        };
    }
}
