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

    public async Task<Order?> GetById(int orderId) => await _orderDAO.GetByIdAsync(orderId);
    public async Task<bool> Exists(int orderId) => await _orderDAO.ExistsAsync(orderId);
}
