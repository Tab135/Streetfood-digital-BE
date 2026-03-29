using BO.DTO.Cart;
using BO.DTO.Order;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using Service.PaymentsService;
using Service.Utils;

namespace Service;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IDishRepository _dishRepository;
    private readonly IUserVoucherRepository _userVoucherRepository;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public CartService(
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IBranchRepository branchRepository,
        IDishRepository dishRepository,
        IUserVoucherRepository userVoucherRepository,
        IOrderService orderService,
        IPaymentService paymentService)
    {
        _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
        _userVoucherRepository = userVoucherRepository ?? throw new ArgumentNullException(nameof(userVoucherRepository));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
    }

    public async Task<CartResponseDto> GetMyCartAsync(int userId)
    {
        await EnsureUserExistsAsync(userId);

        var cart = await _cartRepository.GetByUserIdAsync(userId);
        return cart == null ? CreateEmptyCartDto(userId) : MapToDto(cart);
    }

    public async Task<CartResponseDto> AddItemAsync(int userId, AddCartItemRequest request)
    {
        await EnsureUserExistsAsync(userId);
        await EnsureBranchAllowsOrderingAsync(request.BranchId);

        if (request.Quantity <= 0)
        {
            throw new DomainExceptions("Quantity must be at least 1");
        }

        var dish = await _dishRepository.GetByIdAsync(request.DishId)
            ?? throw new DomainExceptions("Dish not found");

        if (!dish.IsActive)
        {
            throw new DomainExceptions("Dish is not active");
        }

        var branchDish = await _dishRepository.GetBranchDishAsync(request.BranchId, request.DishId);
        if (branchDish == null)
        {
            throw new DomainExceptions("Dish is not available in this branch");
        }

        if (branchDish.IsSoldOut || dish.IsSoldOut)
        {
            throw new DomainExceptions("Dish is currently sold out");
        }

        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart == null)
        {
            cart = await _cartRepository.CreateAsync(new BO.Entities.Cart
            {
                UserId = userId,
                BranchId = request.BranchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (cart.BranchId.HasValue && cart.BranchId.Value != request.BranchId)
        {
            if (cart.Items.Count > 0)
            {
                throw new DomainExceptions("Cart currently contains dishes from another branch. Please clear your cart first.");
            }

            cart.BranchId = request.BranchId;
            await _cartRepository.UpdateAsync(cart);
        }

        if (!cart.BranchId.HasValue)
        {
            cart.BranchId = request.BranchId;
            await _cartRepository.UpdateAsync(cart);
        }

        var existingItem = await _cartRepository.GetItemByDishIdAsync(cart.CartId, request.DishId);
        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.UnitPrice = dish.Price;
            await _cartRepository.UpdateItemAsync(existingItem);
        }
        else
        {
            await _cartRepository.AddItemAsync(new BO.Entities.CartItem
            {
                CartId = cart.CartId,
                DishId = request.DishId,
                Quantity = request.Quantity,
                UnitPrice = dish.Price,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return MapToDto((await _cartRepository.GetByUserIdAsync(userId))!);
    }

    public async Task<CartResponseDto> UpdateItemQuantityAsync(int userId, int dishId, UpdateCartItemRequest request)
    {
        await EnsureUserExistsAsync(userId);

        if (request.Quantity <= 0)
        {
            throw new DomainExceptions("Quantity must be at least 1");
        }

        var cart = await _cartRepository.GetByUserIdAsync(userId)
            ?? throw new DomainExceptions("Cart not found");

        if (cart.BranchId.HasValue)
        {
            await EnsureBranchAllowsOrderingAsync(cart.BranchId.Value);
        }

        var item = await _cartRepository.GetItemByDishIdAsync(cart.CartId, dishId)
            ?? throw new DomainExceptions("Item not found in cart");

        item.Quantity = request.Quantity;

        var dish = await _dishRepository.GetByIdAsync(dishId)
            ?? throw new DomainExceptions("Dish not found");

        item.UnitPrice = dish.Price;
        await _cartRepository.UpdateItemAsync(item);

        return MapToDto((await _cartRepository.GetByUserIdAsync(userId))!);
    }

    public async Task<CartResponseDto> RemoveItemAsync(int userId, int dishId)
    {
        await EnsureUserExistsAsync(userId);

        var cart = await _cartRepository.GetByUserIdAsync(userId)
            ?? throw new DomainExceptions("Cart not found");

        var item = await _cartRepository.GetItemByDishIdAsync(cart.CartId, dishId)
            ?? throw new DomainExceptions("Item not found in cart");

        await _cartRepository.RemoveItemAsync(item);

        var refreshed = await _cartRepository.GetByUserIdAsync(userId);
        if (refreshed == null || refreshed.Items.Count == 0)
        {
            if (refreshed != null)
            {
                refreshed.BranchId = null;
                await _cartRepository.UpdateAsync(refreshed);
                refreshed = await _cartRepository.GetByUserIdAsync(userId);
            }

            return refreshed == null ? CreateEmptyCartDto(userId) : MapToDto(refreshed);
        }

        return MapToDto(refreshed);
    }

    public async Task<CartResponseDto> ClearCartAsync(int userId)
    {
        await EnsureUserExistsAsync(userId);

        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart == null)
        {
            return CreateEmptyCartDto(userId);
        }

        await _cartRepository.ClearItemsAsync(cart.CartId);
        cart.BranchId = null;
        await _cartRepository.UpdateAsync(cart);

        return MapToDto((await _cartRepository.GetByUserIdAsync(userId))!);
    }

    public async Task<CheckoutCartResponseDto> CheckoutAsync(int userId, CheckoutCartRequest request)
    {
        await EnsureUserExistsAsync(userId);

        var cart = await _cartRepository.GetByUserIdAsync(userId)
            ?? throw new DomainExceptions("Cart not found");

        if (!cart.BranchId.HasValue)
        {
            throw new DomainExceptions("Cart branch is not set");
        }

        await EnsureBranchAllowsOrderingAsync(cart.BranchId.Value);

        if (cart.Items.Count == 0)
        {
            throw new DomainExceptions("Cart is empty");
        }

        var cartTotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        decimal discountAmount = 0m;
        BO.Entities.UserVoucher? redeemedUserVoucher = null;

        if (request.UserVoucherId.HasValue)
        {
            var userVoucher = await _userVoucherRepository.GetByIdAsync(request.UserVoucherId.Value)
                ?? throw new DomainExceptions("User voucher not found");

            if (userVoucher.UserId != userId)
            {
                throw new DomainExceptions("You do not own this voucher", "ERR_FORBIDDEN");
            }

            if (!userVoucher.IsAvailable || userVoucher.Quantity <= 0)
            {
                throw new DomainExceptions("Voucher is not available");
            }

            var voucher = userVoucher.Voucher ?? throw new DomainExceptions("Voucher not found");
            if (!voucher.IsActive)
            {
                throw new DomainExceptions("Voucher is inactive");
            }

            var now = DateTime.UtcNow;
            if (now < voucher.StartDate || now > voucher.EndDate)
            {
                throw new DomainExceptions("Voucher is out of valid time range");
            }

            if (voucher.ExpiredDate.HasValue && now > voucher.ExpiredDate.Value)
            {
                throw new DomainExceptions("Voucher has expired");
            }

            if (voucher.MinAmountRequired > cartTotal)
            {
                throw new DomainExceptions("Order amount does not meet voucher minimum requirement");
            }

            discountAmount = VoucherRules.CalculateDiscountAmount(cartTotal, voucher);
            redeemedUserVoucher = userVoucher;
        }

        var createOrderRequest = new CreateOrderRequest
        {
            BranchId = cart.BranchId.Value,
            UserVoucherId = request.UserVoucherId,
            Table = request.Table,
            PaymentMethod = request.PaymentMethod,
            DiscountAmount = discountAmount,
            IsTakeAway = request.IsTakeAway,
            Items = cart.Items.Select(i => new CreateOrderDishRequest
            {
                DishId = i.DishId,
                Quantity = i.Quantity
            }).ToList()
        };

        var order = await _orderService.CreateOrderAsync(createOrderRequest, userId);
        var payment = await _paymentService.CreateOrderPaymentLink(userId, order.OrderId);

        if (!payment.Success)
        {
            await _orderService.DeleteOrderAsync(order.OrderId, userId);
            throw new DomainExceptions(payment.Message ?? "Failed to create payment link for order");
        }

        if (redeemedUserVoucher != null)
        {
            redeemedUserVoucher.Quantity -= 1;
            if (redeemedUserVoucher.Quantity <= 0)
            {
                redeemedUserVoucher.Quantity = 0;
                redeemedUserVoucher.IsAvailable = false;
            }

            await _userVoucherRepository.UpdateAsync(redeemedUserVoucher);
        }

        // Cart is intentionally NOT cleared here.
        // It will be cleared in PaymentService.ConfirmPaymentFromRedirect once payment is PAID,
        // so the user can re-checkout if they abandon the payment.

        return new CheckoutCartResponseDto
        {
            Order = order,
            Payment = payment
        };
    }

    private async Task EnsureUserExistsAsync(int userId)
    {
        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            throw new DomainExceptions("User not found");
        }
    }

    private async Task EnsureBranchAllowsOrderingAsync(int branchId)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId)
            ?? throw new DomainExceptions("Branch not found");

        if (!branch.IsSubscribed)
        {
            throw new DomainExceptions("This branch is not subscribed and cannot accept cart or order checkout actions.");
        }
    }

    private static CartResponseDto CreateEmptyCartDto(int userId)
    {
        return new CartResponseDto
        {
            UserId = userId,
            TotalAmount = 0m,
            Items = new List<CartItemResponseDto>()
        };
    }

    private static CartResponseDto MapToDto(BO.Entities.Cart cart)
    {
        var items = cart.Items
            .OrderByDescending(i => i.UpdatedAt)
            .Select(i => new CartItemResponseDto
            {
                DishId = i.DishId,
                DishName = i.Dish?.Name ?? string.Empty,
                DishImageUrl = i.Dish?.ImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.UnitPrice * i.Quantity
            })
            .ToList();

        return new CartResponseDto
        {
            CartId = cart.CartId,
            UserId = cart.UserId,
            BranchId = cart.BranchId,
            BranchName = cart.Branch?.Name,
            TotalAmount = items.Sum(i => i.LineTotal),
            Items = items
        };
    }
}
