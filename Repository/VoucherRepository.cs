using BO.Entities;
using DAL;
using Repository.Interfaces;

namespace Repository;

public class VoucherRepository : IVoucherRepository
{
    private readonly VoucherDAO _voucherDAO;

    public VoucherRepository(VoucherDAO voucherDAO)
    {
        _voucherDAO = voucherDAO ?? throw new ArgumentNullException(nameof(voucherDAO));
    }

    public Task<List<Voucher>> CreateRangeAsync(List<Voucher> vouchers) => _voucherDAO.CreateRangeAsync(vouchers);

    public Task<Voucher?> GetByIdAsync(int voucherId) => _voucherDAO.GetByIdAsync(voucherId);

    public Task<Voucher?> GetByCodeAsync(string voucherCode) => _voucherDAO.GetByCodeAsync(voucherCode);

    public Task<List<Voucher>> GetAllAsync(bool? isBelongAQuestTask = null, bool? isRemaining = null) => _voucherDAO.GetAllAsync(isBelongAQuestTask, isRemaining);

    public Task UpdateAsync(Voucher voucher) => _voucherDAO.UpdateAsync(voucher);

    public Task DeleteAsync(int voucherId) => _voucherDAO.DeleteAsync(voucherId);

    public Task<bool> ExistsByIdAsync(int voucherId) => _voucherDAO.ExistsByIdAsync(voucherId);

    public Task<List<Voucher>> GetByCampaignIdAsync(int campaignId) => _voucherDAO.GetByCampaignIdAsync(campaignId);
    public Task<List<Voucher>> GetByCampaignIdsAsync(List<int> campaignIds) => _voucherDAO.GetByCampaignIdsAsync(campaignIds);
    public Task<List<Voucher>> GetMarketplaceVouchersAsync(DateTime now) => _voucherDAO.GetMarketplaceVouchersAsync(now);
}
