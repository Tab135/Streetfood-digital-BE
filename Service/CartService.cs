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
    private readonly IVoucherRepository _voucherRepository;
    private readonly IUserVoucherRepository _userVoucherRepository;
    private readonly IBranchCampaignRepository _branchCampaignRepository;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public CartService(
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IBranchRepository branchRepository,
        IDishRepository dishRepository,
        IVoucherRepository voucherRepository,
        IUserVoucherRepository userVoucherRepository,
        IBranchCampaignRepository branchCampaignRepository,
        IOrderService orderService,
        IPaymentService paymentService)
    {
        _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
        _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
        _voucherRepository = voucherRepository ?? throw new ArgumentNullException(nameof(voucherRepository));
        _userVoucherRepository = userVoucherRepository ?? throw new ArgumentNullException(nameof(userVoucherRepository));
        _branchCampaignRepository = branchCampaignRepository ?? throw new ArgumentNullException(nameof(branchCampaignRepository));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
    }

    public async Task<List<CartResponseDto>> GetMyCartsAsync(int userId)
    {
        await EnsureUserExistsAsync(userId);

        var carts = await _cartRepository.GetByUserIdAllAsync(userId);
        return carts
            .Where(c => c.Items.Count > 0)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<CartResponseDto> GetMyCartByBranchAsync(int userId, int branchId)
    {
        await EnsureUserExistsAsync(userId);

        var cart = await _cartRepository.GetByUserAndBranchAsync(userId, branchId);
        return cart == null ? CreateEmptyCartDto(userId, branchId) : MapToDto(cart);
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

        if (branchDish.IsSoldOut)
        {
            throw new DomainExceptions("Dish is currently sold out");
        }

        var cart = await _cartRepository.GetByUserAndBranchAsync(userId, request.BranchId);
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

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.UpdateAsync(cart);

        return MapToDto((await _cartRepository.GetByUserAndBranchAsync(userId, request.BranchId))!);
    }

    public async Task<CartResponseDto> UpdateItemQuantityAsync(int userId, int branchId, int dishId, UpdateCartItemRequest request)
    {
        await EnsureUserExistsAsync(userId);

        if (request.Quantity <= 0)
        {
            throw new DomainExceptions("Quantity must be at least 1");
        }

        var cart = await _cartRepository.GetByUserAndBranchAsync(userId, branchId)
            ?? throw new DomainExceptions("Cart not found");

        await EnsureBranchAllowsOrderingAsync(branchId);

        var item = await _cartRepository.GetItemByDishIdAsync(cart.CartId, dishId)
            ?? throw new DomainExceptions("Item not found in cart");

        item.Quantity = request.Quantity;

        var dish = await _dishRepository.GetByIdAsync(dishId)
            ?? throw new DomainExceptions("Dish not found");

        item.UnitPrice = dish.Price;
        await _cartRepository.UpdateItemAsync(item);

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.UpdateAsync(cart);

        return MapToDto((await _cartRepository.GetByUserAndBranchAsync(userId, branchId))!);
    }

    public async Task<CartResponseDto> RemoveItemAsync(int userId, int branchId, int dishId)
    {
        await EnsureUserExistsAsync(userId);

        var cart = await _cartRepository.GetByUserAndBranchAsync(userId, branchId)
            ?? throw new DomainExceptions("Cart not found");

        var item = await _cartRepository.GetItemByDishIdAsync(cart.CartId, dishId)
            ?? throw new DomainExceptions("Item not found in cart");

        await _cartRepository.RemoveItemAsync(item);

        var refreshed = await _cartRepository.GetByUserAndBranchAsync(userId, branchId);
        if (refreshed == null || refreshed.Items.Count == 0)
        {
            if (refreshed != null)
            {
                await _cartRepository.DeleteAsync(refreshed.CartId);
            }

            return CreateEmptyCartDto(userId, branchId);
        }

        refreshed.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.UpdateAsync(refreshed);

        return MapToDto(refreshed);
    }

    public async Task<CartResponseDto> ClearCartAsync(int userId, int branchId)
    {
        await EnsureUserExistsAsync(userId);

        var cart = await _cartRepository.GetByUserAndBranchAsync(userId, branchId);
        if (cart == null)
        {
            return CreateEmptyCartDto(userId, branchId);
        }

        await _cartRepository.DeleteAsync(cart.CartId);

        return CreateEmptyCartDto(userId, branchId);
    }

    public async Task<CheckoutCartResponseDto> CheckoutAsync(int userId, CheckoutCartRequest request)
    {
        await EnsureUserExistsAsync(userId);

        if (request.BranchId <= 0)
        {
            throw new DomainExceptions("BranchId is required for checkout");
        }

        var cart = await _cartRepository.GetByUserAndBranchAsync(userId, request.BranchId)
            ?? throw new DomainExceptions("Cart not found");

        await EnsureBranchAllowsOrderingAsync(request.BranchId);

        if (cart.Items.Count == 0)
        {
            throw new DomainExceptions("Cart is empty");
        }

        var cartTotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        decimal discountAmount = 0m;
        BO.Entities.UserVoucher? redeemedUserVoucher = null;
        BO.Entities.Voucher? redeemedVendorVoucher = null;

        if (request.VoucherId.HasValue)
        {
            var voucher = await _voucherRepository.GetByIdAsync(request.VoucherId.Value)
                ?? throw new DomainExceptions("Voucher not found");

            if (!voucher.IsActive)
            {
                throw new DomainExceptions("Voucher is inactive");
            }

            var now = DateTime.UtcNow;
            VoucherRules.EnsureVoucherIsWithinValidDateRange(voucher, now);

            if (VoucherRules.IsOutOfStock(voucher))
            {
                throw new DomainExceptions("Voucher is out of stock");
            }

            if (voucher.VendorCampaignId.HasValue)
            {
                var campaign = voucher.VendorCampaign
                    ?? throw new DomainExceptions("Campaign voucher is invalid");

                var joinInfo = await _branchCampaignRepository.GetByBranchAndCampaignAsync(request.BranchId, campaign.CampaignId);
                if (joinInfo == null || joinInfo.IsActive != true)
                {
                    if (campaign.CreatedByVendorId.HasValue)
                    {
                        throw new DomainExceptions("This branch is not included in this vendor campaign.");
                    }

                    throw new DomainExceptions("This voucher campaign is not active for this branch.");
                }

                if (campaign.CreatedByVendorId.HasValue)
                {
                    redeemedVendorVoucher = voucher;
                }
            }

            if (redeemedVendorVoucher == null)
            {
                var userVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, voucher.VoucherId)
                    ?? throw new DomainExceptions("You have not claimed this voucher yet");

                if (!userVoucher.IsAvailable || userVoucher.Quantity <= 0)
                {
                    throw new DomainExceptions("Voucher is not available");
                }

                redeemedUserVoucher = userVoucher;
            }

            if (voucher.MinAmountRequired > cartTotal)
            {
                throw new DomainExceptions("Order amount does not meet voucher minimum requirement");
            }

            discountAmount = VoucherRules.CalculateDiscountAmount(cartTotal, voucher);
        }

        var createOrderRequest = new CreateOrderRequest
        {
            BranchId = request.BranchId,
            AppliedVoucherId = request.VoucherId,
            Table = request.Table,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note,
            DiscountAmount = discountAmount,
            IsTakeAway = request.IsTakeAway,
            Items = cart.Items.Select(i => new CreateOrderDishRequest
            {
                DishId = i.DishId,
                Quantity = i.Quantity
            }).ToList()
        };

        var (order, createdNewOrder, previousAppliedVoucherId) = await _orderService.CreateOrUpdatePendingOrderForCartAsync(createOrderRequest, userId);

        if (!createdNewOrder)
        {
            await ReconcileVoucherUsageAfterCheckoutAsync(
                userId,
                request.VoucherId,
                previousAppliedVoucherId,
                false,
                redeemedUserVoucher,
                redeemedVendorVoucher);
        }

        var payment = await _paymentService.CreateOrderPaymentLink(userId, order.OrderId);

        if (!payment.Success)
        {
            if (createdNewOrder)
            {
                await _orderService.DeleteOrderAsync(order.OrderId, userId);
            }

            throw new DomainExceptions(payment.Message ?? "Failed to create payment link for order");
        }

        if (createdNewOrder)
        {
            await ReconcileVoucherUsageAfterCheckoutAsync(
                userId,
                request.VoucherId,
                previousAppliedVoucherId,
                true,
                redeemedUserVoucher,
                redeemedVendorVoucher);
        }

        return new CheckoutCartResponseDto
        {
            Order = order,
            Payment = payment
        };
    }

    private async Task ReconcileVoucherUsageAfterCheckoutAsync(
        int userId,
        int? requestedVoucherId,
        int? previousAppliedVoucherId,
        bool createdNewOrder,
        BO.Entities.UserVoucher? selectedUserVoucher,
        BO.Entities.Voucher? selectedVendorVoucher)
    {
        if (createdNewOrder)
        {
            await ConsumeSelectedVoucherUsageAsync(selectedUserVoucher, selectedVendorVoucher);
            return;
        }

        if (previousAppliedVoucherId == requestedVoucherId)
        {
            return;
        }

        if (previousAppliedVoucherId.HasValue)
        {
            await RestoreVoucherUsageAsync(userId, previousAppliedVoucherId.Value);
        }

        if (!requestedVoucherId.HasValue)
        {
            return;
        }

        if (selectedUserVoucher == null && selectedVendorVoucher == null)
        {
            var selectedVoucher = await _voucherRepository.GetByIdAsync(requestedVoucherId.Value)
                ?? throw new DomainExceptions("Voucher not found");

            if (IsSystemFundedVoucher(selectedVoucher))
            {
                selectedUserVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, selectedVoucher.VoucherId)
                    ?? throw new DomainExceptions("You have not claimed this voucher yet");
            }
            else
            {
                selectedVendorVoucher = selectedVoucher;
            }
        }

        await ConsumeSelectedVoucherUsageAsync(selectedUserVoucher, selectedVendorVoucher);
    }

    private async Task ConsumeSelectedVoucherUsageAsync(
        BO.Entities.UserVoucher? selectedUserVoucher,
        BO.Entities.Voucher? selectedVendorVoucher)
    {
        if (selectedUserVoucher != null)
        {
            selectedUserVoucher.Quantity -= 1;
            if (selectedUserVoucher.Quantity <= 0)
            {
                selectedUserVoucher.Quantity = 0;
                selectedUserVoucher.IsAvailable = false;
            }

            await _userVoucherRepository.UpdateAsync(selectedUserVoucher);
        }

        if (selectedVendorVoucher != null)
        {
            selectedVendorVoucher.UsedQuantity += 1;
            if (VoucherRules.IsOutOfStock(selectedVendorVoucher))
            {
                throw new DomainExceptions("Voucher is out of stock");
            }

            await _voucherRepository.UpdateAsync(selectedVendorVoucher);
        }
    }

    private async Task RestoreVoucherUsageAsync(int userId, int voucherId)
    {
        var voucher = await _voucherRepository.GetByIdAsync(voucherId)
            ?? throw new DomainExceptions("Voucher not found for checkout update");

        if (IsSystemFundedVoucher(voucher))
        {
            var userVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, voucher.VoucherId)
                ?? throw new DomainExceptions("Claimed voucher not found for this user");

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

    private static bool IsSystemFundedVoucher(BO.Entities.Voucher voucher)
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

    private static CartResponseDto CreateEmptyCartDto(int userId, int? branchId = null)
    {
        return new CartResponseDto
        {
            UserId = userId,
            BranchId = branchId,
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
