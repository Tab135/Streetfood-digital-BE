using BO.Entities;

namespace Repository.Interfaces;

public interface IOrderRepository
{
    Task<Order> Create(Order order, List<OrderDish> orderDishes);
    Task<Order?> GetById(int orderId);
    Task<(List<Order> items, int totalCount)> GetByUserId(int userId, int pageNumber, int pageSize, List<OrderStatus>? statuses = null);
    Task<Order?> GetLatestPendingByUserAndBranch(int userId, int branchId);
    Task<List<Order>> GetPendingOrdersNotUpdatedSince(DateTime staleBeforeUtc);
    Task<(List<Order> items, int totalCount)> GetByBranchIds(List<int> branchIds, int pageNumber, int pageSize, List<OrderStatus>? statuses = null);
    Task<Order> Update(Order order, List<OrderDish>? orderDishes = null);
    Task Delete(int orderId);
    Task<bool> Exists(int orderId);
}
