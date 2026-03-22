using BO.DTO.Voucher;

namespace Service.Interfaces;

public interface IVoucherService
{
    Task<VoucherDto> CreateVoucherAsync(CreateVoucherDto createDto, int userId);
    Task<VoucherDto?> GetVoucherByIdAsync(int voucherId);
    Task<List<VoucherDto>> GetAllVouchersAsync();
    Task<VoucherDto> UpdateVoucherAsync(int voucherId, UpdateVoucherDto updateDto, int userId);
    Task<bool> DeleteVoucherAsync(int voucherId, int userId);
}
