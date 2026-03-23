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

    public async Task<UserVoucher?> GetByUserAndVoucherAsync(int userId, int voucherId)
    {
        return await _context.UserVouchers
            .Include(uv => uv.Voucher)
            .FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
    }

    public async Task<IEnumerable<UserVoucher>> GetByUserIdAsync(int userId)
    {
        return await _context.UserVouchers
            .Include(uv => uv.Voucher)
            .Where(uv => uv.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserVoucher> CreateAsync(UserVoucher userVoucher)
    {
        _context.UserVouchers.Add(userVoucher);
        await _context.SaveChangesAsync();
        return userVoucher;
    }

    public async Task UpdateAsync(UserVoucher userVoucher)
    {
        _context.UserVouchers.Update(userVoucher);
        await _context.SaveChangesAsync();
    }
}
