using BO.Entities;

namespace Repository.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetById(int orderId);
    Task<bool> Exists(int orderId);
}
