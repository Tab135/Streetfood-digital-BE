using BO.Entities;

namespace Repository.Interfaces;

public interface IUserVoucherRepository
{
    Task<UserVoucher?> GetByIdAsync(int userVoucherId);
    Task UpdateAsync(UserVoucher userVoucher);
}
