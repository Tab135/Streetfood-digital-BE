using BO.Entities;

namespace Repository.Interfaces;

public interface IVoucherRepository
{
    Task<List<Voucher>> CreateRangeAsync(List<Voucher> vouchers);
    Task<Voucher?> GetByIdAsync(int voucherId);
    Task<Voucher?> GetByCodeAsync(string voucherCode);
    Task<List<Voucher>> GetAllAsync();
    Task UpdateAsync(Voucher voucher);
    Task DeleteAsync(int voucherId);
    Task<bool> ExistsByIdAsync(int voucherId);
    Task<List<Voucher>> GetByCampaignIdAsync(int campaignId);
    Task<List<Voucher>> GetByCampaignIdsAsync(List<int> campaignIds);
    Task<List<Voucher>> GetMarketplaceVouchersAsync(DateTime now);
}
