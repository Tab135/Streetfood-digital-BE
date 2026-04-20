using BO.Entities;

namespace Repository.Interfaces;

public interface IUserVoucherRepository
{
    Task<UserVoucher?> GetByIdAsync(int userVoucherId);
    Task<UserVoucher?> GetByUserAndVoucherAsync(int userId, int voucherId);
    Task<bool> HasUsersClaimedVoucherAsync(int voucherId);
    Task<IEnumerable<UserVoucher>> GetByUserIdAsync(int userId);
    Task<UserVoucher> CreateAsync(UserVoucher userVoucher);
    Task UpdateAsync(UserVoucher userVoucher);
}
