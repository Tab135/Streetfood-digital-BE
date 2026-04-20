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

    public async Task<List<Voucher>> CreateRangeAsync(List<Voucher> vouchers)
    {
        _context.Vouchers.AddRange(vouchers);
        await _context.SaveChangesAsync();
        return vouchers;
    }

    public async Task<Voucher?> GetByIdAsync(int voucherId)
    {
        return await _context.Vouchers
            .Include(v => v.VendorCampaign)
            .FirstOrDefaultAsync(v => v.VoucherId == voucherId);
    }

    public async Task<Voucher?> GetByCodeAsync(string voucherCode)
    {
        return await _context.Vouchers.FirstOrDefaultAsync(v => v.VoucherCode == voucherCode);
    }

    public async Task<List<Voucher>> GetAllAsync(bool? isBelongAQuestTask = null, bool? isRemaining = null)
    {
        var query = _context.Vouchers.Include(v => v.VendorCampaign).AsQueryable();

        if (isRemaining.HasValue)
        {
            if (isRemaining.Value)
            {
                query = query.Where(v => v.UsedQuantity < v.Quantity);
            }
            else
            {
                query = query.Where(v => v.UsedQuantity >= v.Quantity);
            }
        }

        if (isBelongAQuestTask.HasValue)
        {
            if (isBelongAQuestTask.Value)
            {
                query = query.Where(v => _context.QuestTaskRewards
                    .Any(q => q.RewardType == BO.Enums.QuestRewardType.VOUCHER && q.RewardValue == v.VoucherId));
            }
            else
            {
                query = query.Where(v => !_context.QuestTaskRewards
                    .Any(q => q.RewardType == BO.Enums.QuestRewardType.VOUCHER && q.RewardValue == v.VoucherId));
            }
        }

        return await query
            .OrderByDescending(v => v.VoucherId)
            .ToListAsync();
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
            .Where(v => v.VendorCampaignId == campaignId || 
                        _context.QuestTaskRewards
                            .Any(qtr => qtr.RewardType == BO.Enums.QuestRewardType.VOUCHER &&
                                        qtr.RewardValue == v.VoucherId &&
                                        qtr.QuestTask.Quest.CampaignId == campaignId))
            .OrderByDescending(v => v.VoucherId)
            .ToListAsync();
    }

    public async Task<List<Voucher>> GetMarketplaceVouchersAsync(DateTime now)
    {
        return await _context.Vouchers
            .Where(v => v.IsActive && v.StartDate <= now && (!v.EndDate.HasValue || v.EndDate >= now) && v.VendorCampaignId == null && v.RedeemPoint > 0)
            .OrderByDescending(v => v.VoucherId)
            .ToListAsync();
    }

    public async Task<List<Voucher>> GetByCampaignIdsAsync(List<int> campaignIds)
    {
        return await _context.Vouchers
            .AsNoTracking()
            .Where(v => v.IsActive && 
                        ((v.VendorCampaignId.HasValue && campaignIds.Contains(v.VendorCampaignId.Value)) ||
                         _context.QuestTaskRewards
                             .Any(qtr => qtr.RewardType == BO.Enums.QuestRewardType.VOUCHER &&
                                         qtr.RewardValue == v.VoucherId &&
                                         qtr.QuestTask.Quest.CampaignId.HasValue &&
                                         campaignIds.Contains(qtr.QuestTask.Quest.CampaignId.Value))))
            .ToListAsync();
    }
}
