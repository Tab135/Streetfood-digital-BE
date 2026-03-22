using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class UserVoucherDAO
{
    private readonly StreetFoodDbContext _context;

    public UserVoucherDAO(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserVoucher?> GetByIdAsync(int userVoucherId)
    {
        return await _context.UserVouchers
            .Include(uv => uv.Voucher)
            .FirstOrDefaultAsync(uv => uv.UserVoucherId == userVoucherId);
    }

    public async Task UpdateAsync(UserVoucher userVoucher)
    {
        _context.UserVouchers.Update(userVoucher);
        await _context.SaveChangesAsync();
    }
}
