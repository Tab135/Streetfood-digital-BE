using BO.Common;
using BO.DTO.Order;
using BO.Entities;
using System.Threading;

namespace Service.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequest request, int userId);
    Task<PaginatedResponse<AdminOrderResponseDto>> GetAllOrdersForAdminAsync(int pageNumber, int pageSize, OrderStatus? status = null, int? branchId = null, int? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<(OrderResponseDto order, bool createdNew, int? previousAppliedVoucherId)> CreateOrUpdatePendingOrderForCartAsync(CreateOrderRequest request, int userId);
    Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId);
    Task<PaginatedResponse<OrderResponseDto>> GetMyOrdersAsync(int userId, int pageNumber, int pageSize, OrderStatus? status = null);
    Task<PaginatedResponse<OrderResponseDto>> GetVendorOrdersAsync(int vendorUserId, int pageNumber, int pageSize, OrderStatus? status = null);
    Task<PaginatedResponse<OrderResponseDto>> GetVendorOrdersByBranchAsync(int vendorUserId, int branchId, int pageNumber, int pageSize, OrderStatus? status = null);
    Task<PaginatedResponse<OrderResponseDto>> GetManagerOrdersAsync(int managerUserId, int pageNumber, int pageSize, OrderStatus? status = null);
    Task<OrderPickupCodeResponseDto> GetOrderPickupCodeAsync(int orderId, int userId);
    Task<OrderResponseDto> CancelOrderAsync(int orderId, int userId);
    Task<OrderResponseDto> UpdateOrderAsync(int orderId, UpdateOrderRequest request, int userId);
    Task<OrderResponseDto> VendorDecideOrderAsync(int orderId, int vendorUserId, bool approve);
    Task<OrderResponseDto> VendorCompleteOrderAsync(int orderId, int vendorUserId, string verificationCode);
    Task AutoCompleteUnpickedOrderAsync(int orderId);
    Task<int> CancelAbandonedPendingOrdersAsync(TimeSpan inactivityTimeout, CancellationToken cancellationToken = default);
    Task<bool> DeleteOrderAsync(int orderId, int userId);
}
