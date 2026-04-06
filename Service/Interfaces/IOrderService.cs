using BO.Common;
using BO.DTO.Order;
using BO.Entities;

namespace Service.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderRequest request, int userId);
    Task<(OrderResponseDto order, bool createdNew)> CreateOrUpdatePendingOrderForCartAsync(CreateOrderRequest request, int userId);
    Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId);
    Task<PaginatedResponse<OrderResponseDto>> GetMyOrdersAsync(int userId, int pageNumber, int pageSize, OrderStatus? status = null);
    Task<PaginatedResponse<OrderResponseDto>> GetVendorOrdersAsync(int vendorUserId, int pageNumber, int pageSize, OrderStatus? status = null);
    Task<PaginatedResponse<OrderResponseDto>> GetVendorOrdersByBranchAsync(int vendorUserId, int branchId, int pageNumber, int pageSize, OrderStatus? status = null);
    Task<PaginatedResponse<OrderResponseDto>> GetManagerOrdersAsync(int managerUserId, int pageNumber, int pageSize, OrderStatus? status = null);
    Task<OrderPickupCodeResponseDto> GetOrderPickupCodeAsync(int orderId, int userId);
    Task<OrderResponseDto> UpdateOrderAsync(int orderId, UpdateOrderRequest request, int userId);
    Task<OrderResponseDto> VendorDecideOrderAsync(int orderId, int vendorUserId, bool approve);
    Task<OrderResponseDto> VendorCompleteOrderAsync(int orderId, int vendorUserId, string verificationCode);
    Task<bool> DeleteOrderAsync(int orderId, int userId);
}
