using BO.Entities;
using DAL;
using Repository.Interfaces;

namespace Repository;

public class UserPinRepository : IUserPinRepository
{
    private readonly UserPinDAO _userPinDAO;

    public UserPinRepository(UserPinDAO userPinDAO)
    {
        _userPinDAO = userPinDAO ?? throw new ArgumentNullException(nameof(userPinDAO));
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _userPinDAO.GetByIdAsync(userId);
    }

    public async Task UpdateAsync(User user)
    {
        await _userPinDAO.UpdateAsync(user);
    }
}