using BO.DTO.Voucher;

namespace Service.Interfaces;

public interface IVoucherService
{
    Task<List<CreateVoucherResponseDto>> CreateVouchersAsync(List<CreateVoucherDto> createDtos, int userId);
    Task<VoucherDto?> GetVoucherByIdAsync(int voucherId);
    Task<List<VoucherDto>> GetAllVouchersAsync(bool? isBelongAQuestTask = null, bool? isRemaining = null, bool? isSystemVoucher = null);
    Task<VoucherDto> UpdateVoucherAsync(int voucherId, UpdateVoucherDto updateDto, int userId);
    Task<bool> DeleteVoucherAsync(int voucherId, int userId);
    Task<ClaimVoucherResponseDto> ClaimVoucherAsync(int voucherId, int userId);
    Task<List<UserVoucherResponseDto>> GetUserVouchersAsync(int userId);
    Task<List<VoucherDto>> GetVouchersByCampaignIdAsync(int campaignId);
    Task<List<UserVoucherResponseDto>> GetApplicableUserVouchersAsync(int userId, int branchId);
    Task<List<VoucherDto>> GetMarketplaceVouchersAsync();
}
