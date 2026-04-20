using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BO.DTO.Order;
using BO.Entities;
using BO.Enums;
using BO.Exceptions;
using BO.Common;
using Moq;
using Repository.Interfaces;
using Service;
using Service.Interfaces;
using Service.PaymentsService;
using Xunit;

namespace StreetFood.Tests.OrderTests
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly Mock<IBranchRepository> _branchRepoMock;
        private readonly Mock<IDishRepository> _dishRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IVendorRepository> _vendorRepoMock;
        private readonly Mock<IVoucherRepository> _voucherRepoMock;
        private readonly Mock<IUserVoucherRepository> _userVoucherRepoMock;
        private readonly Mock<IBranchCampaignRepository> _branchCampaignRepoMock;
        private readonly Mock<INotificationService> _notifServiceMock;
        private readonly Mock<IQuestProgressService> _questServiceMock;
        private readonly Mock<ISettingService> _settingServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IPaymentService> _paymentServiceMock;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _orderRepoMock = new Mock<IOrderRepository>();
            _branchRepoMock = new Mock<IBranchRepository>();
            _dishRepoMock = new Mock<IDishRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _vendorRepoMock = new Mock<IVendorRepository>();
            _voucherRepoMock = new Mock<IVoucherRepository>();
            _userVoucherRepoMock = new Mock<IUserVoucherRepository>();
            _branchCampaignRepoMock = new Mock<IBranchCampaignRepository>();
            _notifServiceMock = new Mock<INotificationService>();
            _questServiceMock = new Mock<IQuestProgressService>();
            _settingServiceMock = new Mock<ISettingService>();
            _userServiceMock = new Mock<IUserService>();
            _paymentServiceMock = new Mock<IPaymentService>();

            _orderService = new OrderService(
                _orderRepoMock.Object,
                _branchRepoMock.Object,
                _dishRepoMock.Object,
                _userRepoMock.Object,
                _vendorRepoMock.Object,
                _voucherRepoMock.Object,
                _userVoucherRepoMock.Object,
                _branchCampaignRepoMock.Object,
                _notifServiceMock.Object,
                _questServiceMock.Object,
                _settingServiceMock.Object,
                _userServiceMock.Object,
                _paymentServiceMock.Object
            );
        }

        // --- SECTION: CREATE ORDER (7 TEST CASES) ---

        // SV_ORDER_01 (UTCID01) - Normal Success
        [Fact]
        public async Task CreateOrderAsync_Normal_Success_ReturnsResponse()
        {
            var userId = 1; var branchId = 5; var dishId = 10;
            var request = new CreateOrderRequest
            {
                BranchId = branchId,
                Items = new List<CreateOrderDishRequest> { new CreateOrderDishRequest { DishId = dishId, Quantity = 2 } },
                DiscountAmount = 0
            };

            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, IsSubscribed = true });
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, dishId)).ReturnsAsync(new BranchDish { BranchId = branchId, DishId = dishId, IsSoldOut = false });
            _dishRepoMock.Setup(r => r.GetByIdAsync(dishId)).ReturnsAsync(new BO.Entities.Dish { DishId = dishId, Price = 50000, Name = "Ramen" });
            
            _orderRepoMock.Setup(r => r.Create(It.IsAny<BO.Entities.Order>(), It.IsAny<List<OrderDish>>()))
                .ReturnsAsync((BO.Entities.Order o, List<OrderDish> items) => { o.OrderId = 100; o.OrderDishes = items; return o; });

            var result = await _orderService.CreateOrderAsync(request, userId);

            Assert.Equal(100, result.OrderId);
            Assert.Equal(100000, result.TotalAmount);
            Assert.Single(result.Items);
        }

        // SV_ORDER_01 (UTCID02) - Branch Not Found
        [Fact]
        public async Task CreateOrderAsync_BranchNotFound_ThrowsException()
        {
            var userId = 1;
            var request = new CreateOrderRequest { BranchId = 99 };
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((BO.Entities.Branch?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.CreateOrderAsync(request, userId));
            Assert.Contains("Branch not found", ex.Message);
        }

        // SV_ORDER_01 (UTCID03) - Branch Not Subscribed
        [Fact]
        public async Task CreateOrderAsync_BranchNotSubscribed_ThrowsException()
        {
            var userId = 1; var branchId = 5;
            var request = new CreateOrderRequest { BranchId = branchId };
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, IsSubscribed = false });

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.CreateOrderAsync(request, userId));
            Assert.Contains("not subscribed", ex.Message);
        }

        // SV_ORDER_01 (UTCID04) - Items Empty
        [Fact]
        public async Task CreateOrderAsync_ItemsEmpty_ThrowsException()
        {
            var userId = 1; var branchId = 5;
            var request = new CreateOrderRequest { BranchId = branchId, Items = new List<CreateOrderDishRequest>() };
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, IsSubscribed = true });

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.CreateOrderAsync(request, userId));
            Assert.Contains("ít nhất một món ăn", ex.Message.ToLower());
        }

        // SV_ORDER_01 (UTCID05) - Dish Missing in Branch
        [Fact]
        public async Task CreateOrderAsync_DishMissingInBranch_ThrowsException()
        {
            var userId = 1; var branchId = 5;
            var request = new CreateOrderRequest { BranchId = branchId, Items = new List<CreateOrderDishRequest> { new CreateOrderDishRequest { DishId = 10, Quantity = 1 } } };
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, IsSubscribed = true });
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, 10)).ReturnsAsync((BranchDish?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.CreateOrderAsync(request, userId));
            Assert.Contains("không có trong chi nhánh", ex.Message);
        }

        // SV_ORDER_01 (UTCID06) - Dish Sold Out
        [Fact]
        public async Task CreateOrderAsync_DishSoldOut_ThrowsException()
        {
            var userId = 1; var branchId = 5;
            var request = new CreateOrderRequest { BranchId = branchId, Items = new List<CreateOrderDishRequest> { new CreateOrderDishRequest { DishId = 10, Quantity = 1 } } };
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, IsSubscribed = true });
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, 10)).ReturnsAsync(new BranchDish { BranchId = branchId, DishId = 10, IsSoldOut = true });

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.CreateOrderAsync(request, userId));
            Assert.Contains("đã hết", ex.Message);
        }

        // SV_ORDER_01 (UTCID07) - Negative Discount
        [Fact]
        public async Task CreateOrderAsync_NegativeDiscount_ThrowsException()
        {
            var userId = 1; var branchId = 5;
            var request = new CreateOrderRequest 
            { 
                BranchId = branchId, 
                Items = new List<CreateOrderDishRequest> { new CreateOrderDishRequest { DishId = 10, Quantity = 1 } },
                DiscountAmount = -5000 
            };
            _userRepoMock.Setup(r => r.GetUserById(userId)).ReturnsAsync(new User { Id = userId });
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(new BO.Entities.Branch { BranchId = branchId, IsSubscribed = true });
            _dishRepoMock.Setup(r => r.GetBranchDishAsync(branchId, 10)).ReturnsAsync(new BranchDish { BranchId = branchId, DishId = 10, IsSoldOut = false });
            _dishRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new BO.Entities.Dish { DishId = 10, Price = 10000 });

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.CreateOrderAsync(request, userId));
            Assert.Contains("non-negative", ex.Message);
        }
        // --- SECTION: VENDOR DECIDE ORDER (7 TEST CASES) ---

        // SV_ORDER_02 (UTCID01) - Approve Success
        [Fact]
        public async Task VendorDecideOrderAsync_Approve_Success()
        {
            var orderId = 100; var vendorUserId = 10; var branchId = 5; var vendorId = 2;
            var order = new BO.Entities.Order { OrderId = orderId, BranchId = branchId, Status = OrderStatus.AwaitingVendorConfirmation };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = vendorUserId };
            var vendor = new BO.Entities.Vendor { VendorId = vendorId, UserId = vendorUserId };

            _orderRepoMock.Setup(r => r.GetById(orderId)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(vendor);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<BO.Entities.Order>(), It.IsAny<List<OrderDish>?>())).ReturnsAsync(order);

            var result = await _orderService.VendorDecideOrderAsync(orderId, vendorUserId, true);

            Assert.Equal(OrderStatus.Paid, result.Status);
            _orderRepoMock.Verify(r => r.Update(It.Is<BO.Entities.Order>(o => o.Status == OrderStatus.Paid), It.IsAny<List<OrderDish>?>()), Times.Once);
            _notifServiceMock.Verify(r => r.NotifyAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<object?>()), Times.Once);
        }

        // SV_ORDER_02 (UTCID02) - Reject Success (With Refund)
        [Fact]
        public async Task VendorDecideOrderAsync_Reject_Success_RefundsUser()
        {
            var orderId = 100; var vendorUserId = 10; var branchId = 5; var vendorId = 2; var customerId = 50;
            var order = new BO.Entities.Order { OrderId = orderId, BranchId = branchId, UserId = customerId, Status = OrderStatus.AwaitingVendorConfirmation, FinalAmount = 100000 };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = vendorUserId };
            var vendor = new BO.Entities.Vendor { VendorId = vendorId, UserId = vendorUserId };
            var customer = new User { Id = customerId, MoneyBalance = 500 };

            _orderRepoMock.Setup(r => r.GetById(orderId)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(vendor);
            _userRepoMock.Setup(r => r.GetUserById(customerId)).ReturnsAsync(customer);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<BO.Entities.Order>(), It.IsAny<List<OrderDish>?>())).ReturnsAsync(order);

            var result = await _orderService.VendorDecideOrderAsync(orderId, vendorUserId, false);

            Assert.Equal(OrderStatus.Cancelled, result.Status);
            Assert.Equal(100500, customer.MoneyBalance);
            _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.MoneyBalance == 100500)), Times.Once);
        }

        // SV_ORDER_02 (UTCID03) - Order Not Found
        [Fact]
        public async Task VendorDecideOrderAsync_OrderNotFound_ThrowsException()
        {
            _orderRepoMock.Setup(r => r.GetById(It.IsAny<int>())).ReturnsAsync((BO.Entities.Order?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorDecideOrderAsync(999, 1, true));
            Assert.Contains("không tồn tại", ex.Message);
        }

        // SV_ORDER_02 (UTCID04) - Branch Not Found
        [Fact]
        public async Task VendorDecideOrderAsync_BranchNotFound_ThrowsException()
        {
            var order = new BO.Entities.Order { OrderId = 100, BranchId = 5 };
            _orderRepoMock.Setup(r => r.GetById(100)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((BO.Entities.Branch?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorDecideOrderAsync(100, 1, true));
            Assert.Contains("Chi nhánh không tồn tại", ex.Message);
        }

        // SV_ORDER_02 (UTCID05) - Unauthorized
        [Fact]
        public async Task VendorDecideOrderAsync_Unauthorized_ThrowsException()
        {
            var order = new BO.Entities.Order { OrderId = 100, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, VendorId = 2, ManagerId = 999 };
            var vendor = new BO.Entities.Vendor { VendorId = 2, UserId = 888 };

            _orderRepoMock.Setup(r => r.GetById(100)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(vendor);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorDecideOrderAsync(100, 1, true));
            Assert.Contains("không sở hữu", ex.Message);
        }

        // SV_ORDER_02 (UTCID06) - Invalid Status (Already Paid)
        [Fact]
        public async Task VendorDecideOrderAsync_InvalidStatus_ThrowsException()
        {
            var vendorUserId = 10; var branchId = 5; var vendorId = 2;
            var order = new BO.Entities.Order { OrderId = 100, BranchId = branchId, Status = OrderStatus.Paid };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = vendorUserId };
            var vendor = new BO.Entities.Vendor { VendorId = vendorId, UserId = vendorUserId };

            _orderRepoMock.Setup(r => r.GetById(100)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(vendor);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorDecideOrderAsync(100, vendorUserId, true));
            Assert.Contains("chờ xác nhận", ex.Message);
        }

        // SV_ORDER_02 (UTCID07) - User for Refund Not Found
        [Fact]
        public async Task VendorDecideOrderAsync_RefundUserNotFound_ThrowsException()
        {
            var vendorUserId = 10; var branchId = 5; var vendorId = 2; var customerId = 50;
            var order = new BO.Entities.Order { OrderId = 100, BranchId = branchId, UserId = customerId, Status = OrderStatus.AwaitingVendorConfirmation };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = vendorUserId };
            var vendor = new BO.Entities.Vendor { VendorId = vendorId, UserId = vendorUserId };

            _orderRepoMock.Setup(r => r.GetById(100)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(vendor);
            _userRepoMock.Setup(r => r.GetUserById(customerId)).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorDecideOrderAsync(100, vendorUserId, false));
            Assert.Contains("Người dùng không tồn tại", ex.Message);
        }
        // --- SECTION: VENDOR COMPLETE ORDER (7 TEST CASES) ---

        // SV_ORDER_03 (UTCID01) - Normal Success
        [Fact]
        public async Task VendorCompleteOrderAsync_Normal_Success()
        {
            var orderId = 100; var vendorUserId = 10; var branchId = 5; var vendorId = 2; var customerId = 50;
            var code = "123456";
            var order = new BO.Entities.Order { OrderId = orderId, BranchId = branchId, UserId = customerId, Status = OrderStatus.Paid, CompletionCode = code, FinalAmount = 100000 };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = vendorUserId };
            var vendor = new BO.Entities.Vendor { VendorId = vendorId, UserId = vendorUserId, MoneyBalance = 0 };

            _orderRepoMock.Setup(r => r.GetById(orderId)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(vendor);
            _settingServiceMock.Setup(s => s.GetInt("orderXP", 0)).Returns(10);
            _orderRepoMock.Setup(r => r.Update(It.IsAny<BO.Entities.Order>(), It.IsAny<List<OrderDish>?>())).ReturnsAsync(order);

            var result = await _orderService.VendorCompleteOrderAsync(orderId, vendorUserId, code);

            Assert.Equal(OrderStatus.Complete, result.Status);
            Assert.Equal(10, order.OrderXP);
            Assert.Equal(100000, vendor.MoneyBalance);
            _userServiceMock.Verify(s => s.AddXPAsync(customerId, 10), Times.Once);
            _vendorRepoMock.Verify(r => r.UpdateAsync(It.Is<BO.Entities.Vendor>(v => v.MoneyBalance == 100000)), Times.Once);
        }

        // SV_ORDER_03 (UTCID02) - Order Not Found
        [Fact]
        public async Task VendorCompleteOrderAsync_OrderNotFound_ThrowsException()
        {
            _orderRepoMock.Setup(r => r.GetById(It.IsAny<int>())).ReturnsAsync((BO.Entities.Order?)null);
            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorCompleteOrderAsync(999, 1, "123"));
            Assert.Contains("không tồn tại", ex.Message);
        }

        // SV_ORDER_03 (UTCID03) - Unauthorized
        [Fact]
        public async Task VendorCompleteOrderAsync_Unauthorized_ThrowsException()
        {
            var order = new BO.Entities.Order { OrderId = 100, BranchId = 5 };
            var branch = new BO.Entities.Branch { BranchId = 5, VendorId = 2, ManagerId = 999 };
            var vendor = new BO.Entities.Vendor { VendorId = 2, UserId = 888 };

            _orderRepoMock.Setup(r => r.GetById(100)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(vendor);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorCompleteOrderAsync(100, 1, "123"));
            Assert.Contains("không sở hữu", ex.Message);
        }

        // SV_ORDER_03 (UTCID04) - Invalid Status (Not Paid)
        [Fact]
        public async Task VendorCompleteOrderAsync_InvalidStatus_ThrowsException()
        {
            var vendorUserId = 10; var branchId = 5; var vendorId = 2;
            var order = new BO.Entities.Order { OrderId = 100, BranchId = branchId, Status = OrderStatus.Pending };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = vendorUserId };
            var vendor = new BO.Entities.Vendor { VendorId = vendorId, UserId = vendorUserId };

            _orderRepoMock.Setup(r => r.GetById(100)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(vendor);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorCompleteOrderAsync(100, vendorUserId, "123"));
            Assert.Contains("được thanh toán trước", ex.Message);
        }

        // SV_ORDER_03 (UTCID05) - Verification Code Empty
        [Fact]
        public async Task VendorCompleteOrderAsync_CodeEmpty_ThrowsException()
        {
            var vendorUserId = 10; var branchId = 5; var vendorId = 2;
            var order = new BO.Entities.Order { OrderId = 100, BranchId = branchId, Status = OrderStatus.Paid };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = vendorUserId };
            var vendor = new BO.Entities.Vendor { VendorId = vendorId, UserId = vendorUserId };

            _orderRepoMock.Setup(r => r.GetById(100)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(vendor);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorCompleteOrderAsync(100, vendorUserId, ""));
            Assert.Contains("xác minh là bắt buộc", ex.Message);
        }

        // SV_ORDER_03 (UTCID06) - Code Mismatch
        [Fact]
        public async Task VendorCompleteOrderAsync_CodeMismatch_ThrowsException()
        {
            var vendorUserId = 10; var branchId = 5; var vendorId = 2;
            var order = new BO.Entities.Order { OrderId = 100, BranchId = branchId, Status = OrderStatus.Paid, CompletionCode = "654321" };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = vendorUserId };
            var vendor = new BO.Entities.Vendor { VendorId = vendorId, UserId = vendorUserId };

            _orderRepoMock.Setup(r => r.GetById(100)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync(vendor);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorCompleteOrderAsync(100, vendorUserId, "123456"));
            Assert.Contains("không hợp lệ", ex.Message);
        }

        // SV_ORDER_03 (UTCID07) - Vendor Not Found (at completion)
        [Fact]
        public async Task VendorCompleteOrderAsync_VendorNotFound_ThrowsException()
        {
            var vendorUserId = 10; var branchId = 5; var vendorId = 2;
            var order = new BO.Entities.Order { OrderId = 100, BranchId = branchId, Status = OrderStatus.Paid, CompletionCode = "123" };
            var branch = new BO.Entities.Branch { BranchId = branchId, VendorId = vendorId, ManagerId = vendorUserId };

            _orderRepoMock.Setup(r => r.GetById(100)).ReturnsAsync(order);
            _branchRepoMock.Setup(r => r.GetByIdAsync(branchId)).ReturnsAsync(branch);
            _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId)).ReturnsAsync((BO.Entities.Vendor?)null);

            var ex = await Assert.ThrowsAsync<DomainExceptions>(() => _orderService.VendorCompleteOrderAsync(100, vendorUserId, "123"));
            Assert.Contains("Chủ quán không tồn tại", ex.Message);
        }
    }
}
