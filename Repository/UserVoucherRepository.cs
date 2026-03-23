using BO.Entities;
using DAL;
using Repository.Interfaces;

namespace Repository;

public class UserVoucherRepository : IUserVoucherRepository
{
    private readonly UserVoucherDAO _userVoucherDAO;

    public UserVoucherRepository(UserVoucherDAO userVoucherDAO)
    {
        _userVoucherDAO = userVoucherDAO ?? throw new ArgumentNullException(nameof(userVoucherDAO));
    }

    public Task<UserVoucher?> GetByIdAsync(int userVoucherId) => _userVoucherDAO.GetByIdAsync(userVoucherId);

    public Task<UserVoucher?> GetByUserAndVoucherAsync(int userId, int voucherId)
        => _userVoucherDAO.GetByUserAndVoucherAsync(userId, voucherId);

    public Task<IEnumerable<UserVoucher>> GetByUserIdAsync(int userId)
        => _userVoucherDAO.GetByUserIdAsync(userId);

    public Task<UserVoucher> CreateAsync(UserVoucher userVoucher)
        => _userVoucherDAO.CreateAsync(userVoucher);

    public Task UpdateAsync(UserVoucher userVoucher) => _userVoucherDAO.UpdateAsync(userVoucher);
}
