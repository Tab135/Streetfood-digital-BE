using BO.Entities;
using DAL;
using Repository.Interfaces;

namespace Repository;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDAO _orderDAO;

    public OrderRepository(OrderDAO orderDAO)
    {
        _orderDAO = orderDAO ?? throw new ArgumentNullException(nameof(orderDAO));
    }

    public async Task<Order> Create(Order order, List<OrderDish> orderDishes) => await _orderDAO.CreateAsync(order, orderDishes);
    public async Task<Order?> GetById(int orderId) => await _orderDAO.GetByIdAsync(orderId);
    public async Task<(List<Order> items, int totalCount)> GetByUserId(int userId, int pageNumber, int pageSize, List<OrderStatus>? statuses = null)
        => await _orderDAO.GetByUserIdAsync(userId, pageNumber, pageSize, statuses);
    public async Task<Order?> GetLatestPendingByUserAndBranch(int userId, int branchId)
        => await _orderDAO.GetLatestPendingByUserAndBranchAsync(userId, branchId);
    public async Task<List<Order>> GetPendingOrdersNotUpdatedSince(DateTime staleBeforeUtc)
        => await _orderDAO.GetPendingOrdersNotUpdatedSinceAsync(staleBeforeUtc);
    public async Task<(List<Order> items, int totalCount)> GetByBranchIds(List<int> branchIds, int pageNumber, int pageSize, List<OrderStatus>? statuses = null)
        => await _orderDAO.GetByBranchIdsAsync(branchIds, pageNumber, pageSize, statuses);
    public async Task<HashSet<int>> GetOrderIdsWithPaymentsAsync(IEnumerable<int> orderIds)
        => await _orderDAO.GetOrderIdsWithPaymentsAsync(orderIds);
    public async Task<(List<Order> items, Dictionary<int, Payment> paymentByOrderId, int totalCount)> GetAllForAdminAsync(int pageNumber, int pageSize, List<OrderStatus>? statuses = null, int? branchId = null, int? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        => await _orderDAO.GetAllForAdminAsync(pageNumber, pageSize, statuses, branchId, userId, fromDate, toDate);
    public async Task<Order> Update(Order order, List<OrderDish>? orderDishes = null) => await _orderDAO.UpdateAsync(order, orderDishes);
    public async Task Delete(int orderId) => await _orderDAO.DeleteAsync(orderId);
    public async Task<bool> Exists(int orderId) => await _orderDAO.ExistsAsync(orderId);
}
