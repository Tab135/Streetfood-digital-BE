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

    public Task UpdateAsync(UserVoucher userVoucher) => _userVoucherDAO.UpdateAsync(userVoucher);
}
