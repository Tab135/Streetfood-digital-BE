using BO.Entities;

namespace Repository.Interfaces;

public interface IUserPinRepository
{
    Task<User?> GetByIdAsync(int userId);
    Task UpdateAsync(User user);
}