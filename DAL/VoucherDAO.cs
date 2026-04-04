using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class VoucherDAO
{
    private readonly StreetFoodDbContext _context;

    public VoucherDAO(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Voucher> CreateAsync(Voucher voucher)
    {
        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();
        return voucher;
    }

    public async Task<Voucher?> GetByIdAsync(int voucherId)
    {
        return await _context.Vouchers.FindAsync(voucherId);
    }

    public async Task<Voucher?> GetByCodeAsync(string voucherCode)
    {
        return await _context.Vouchers.FirstOrDefaultAsync(v => v.VoucherCode == voucherCode);
    }

    public async Task<List<Voucher>> GetAllAsync()
    {
        return await _context.Vouchers.OrderByDescending(v => v.VoucherId).ToListAsync();
    }

    public async Task UpdateAsync(Voucher voucher)
    {
        _context.Vouchers.Update(voucher);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int voucherId)
    {
        var voucher = await _context.Vouchers.FindAsync(voucherId);
        if (voucher == null)
        {
            return;
        }

        _context.Vouchers.Remove(voucher);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByIdAsync(int voucherId)
    {
        return await _context.Vouchers.AnyAsync(v => v.VoucherId == voucherId);
    }

    public async Task<List<Voucher>> GetByCampaignIdAsync(int campaignId)
    {
        return await _context.Vouchers
            .Where(v => v.CampaignId == campaignId)
            .OrderByDescending(v => v.VoucherId)
            .ToListAsync();
    }

    public async Task<List<Voucher>> GetMarketplaceVouchersAsync(DateTime now)
    {
        return await _context.Vouchers
            .Where(v => v.IsActive && v.StartDate <= now && (!v.EndDate.HasValue || v.EndDate >= now) && v.CampaignId == null && v.RedeemPoint > 0)
            .OrderByDescending(v => v.VoucherId)
            .ToListAsync();
    }

    public async Task<List<Voucher>> GetByCampaignIdsAsync(List<int> campaignIds)
    {
        return await _context.Vouchers
            .AsNoTracking()
            .Where(v => v.CampaignId.HasValue && campaignIds.Contains(v.CampaignId.Value) && v.IsActive)
            .ToListAsync();
    }
}
