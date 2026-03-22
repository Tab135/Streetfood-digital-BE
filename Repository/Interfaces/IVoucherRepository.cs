using BO.Entities;

namespace Repository.Interfaces;

public interface IVoucherRepository
{
    Task<Voucher> CreateAsync(Voucher voucher);
    Task<Voucher?> GetByIdAsync(int voucherId);
    Task<Voucher?> GetByCodeAsync(string voucherCode);
    Task<List<Voucher>> GetAllAsync();
    Task UpdateAsync(Voucher voucher);
    Task DeleteAsync(int voucherId);
    Task<bool> ExistsByIdAsync(int voucherId);
}
