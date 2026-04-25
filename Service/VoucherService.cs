using BO.DTO.Voucher;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;
using Service.Utils;

namespace Service;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _voucherRepository;
    private readonly IUserVoucherRepository _userVoucherRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IBranchCampaignRepository _branchCampaignRepository;
    private readonly IBranchRepository _branchRepository;

    public VoucherService(
        IVoucherRepository voucherRepository,
        IUserVoucherRepository userVoucherRepository,
        IUserRepository userRepository,
        ICampaignRepository campaignRepository,
        IBranchCampaignRepository branchCampaignRepository,
        IBranchRepository branchRepository)
    {
        _voucherRepository = voucherRepository ?? throw new ArgumentNullException(nameof(voucherRepository));
        _userVoucherRepository = userVoucherRepository ?? throw new ArgumentNullException(nameof(userVoucherRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _campaignRepository = campaignRepository ?? throw new ArgumentNullException(nameof(campaignRepository));
        _branchCampaignRepository = branchCampaignRepository ?? throw new ArgumentNullException(nameof(branchCampaignRepository));
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
    }

    public async Task<List<CreateVoucherResponseDto>> CreateVouchersAsync(List<CreateVoucherDto> createDtos, int userId)
    {
        _ = userId;

        if (createDtos == null || createDtos.Count == 0)
        {
            throw new DomainExceptions("Cần ít nhất một phiếu giảm giá");
        }

        var duplicateCodes = createDtos
            .Select(dto => dto.VoucherCode?.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .GroupBy(code => code!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateCodes.Count > 0)
        {
            throw new DomainExceptions($"Mã phiếu giảm giá bị trùng lặp: {string.Join(", ", duplicateCodes)}");
        }

        // Validate campaigns
        var uniqueCampaignIds = createDtos
            .Where(dto => dto.CampaignId.HasValue)
            .Select(dto => dto.CampaignId!.Value)
            .Distinct()
            .ToList();

        var validVendorCampaignIds = new HashSet<int>();
        foreach (var campaignId in uniqueCampaignIds)
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId);
            if (campaign != null && campaign.CreatedByVendorId != null)
            {
                validVendorCampaignIds.Add(campaignId);
            }
        }

        var vouchersToCreate = new List<Voucher>(createDtos.Count);

        foreach (var createDto in createDtos)
        {
            ValidateDateRange(createDto.StartDate, createDto.EndDate);

            var normalizedType = VoucherRules.NormalizeDiscountType(createDto.Type);
            VoucherRules.ValidateDiscountValue(normalizedType, createDto.DiscountValue);

            var voucherCode = createDto.VoucherCode?.Trim();
            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                throw new DomainExceptions("Mã phiếu giảm giá là bắt buộc");
            }

            var existed = await _voucherRepository.GetByCodeAsync(voucherCode);
            if (existed != null)
            {
                throw new DomainExceptions($"Mã phiếu giảm giá đã tồn tại: {voucherCode}");
            }

            vouchersToCreate.Add(new Voucher
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Type = normalizedType,
                DiscountValue = createDto.DiscountValue,
                MinAmountRequired = createDto.MinAmountRequired,
                MaxDiscountValue = createDto.MaxDiscountValue,
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate,
                IsActive = createDto.IsActive,
                VoucherCode = voucherCode,
                RedeemPoint = createDto.RedeemPoint,
                Quantity = createDto.Quantity,
                UsedQuantity = 0,
                VendorCampaignId = createDto.CampaignId.HasValue && validVendorCampaignIds.Contains(createDto.CampaignId.Value) 
                    ? createDto.CampaignId.Value 
                    : null
            });
        }

        var created = await _voucherRepository.CreateRangeAsync(vouchersToCreate);
        return created.Select(MapToCreateResponseDto).ToList();
    }

    public async Task<VoucherDto?> GetVoucherByIdAsync(int voucherId)
    {
        var voucher = await _voucherRepository.GetByIdAsync(voucherId);
        if (voucher == null)
        {
            return null;
        }

        var dto = MapToDto(voucher);
        dto.CampaignId = await ResolveCampaignIdAsync(voucher);
        return dto;
    }

    public async Task<List<VoucherDto>> GetAllVouchersAsync(bool? isBelongAQuestTask = null, bool? isRemaining = null, bool? isSystemVoucher = null)
    {
        var vouchers = await _voucherRepository.GetAllAsync(isBelongAQuestTask, isRemaining, isSystemVoucher);
        var independentVoucherIds = new HashSet<int>(await _voucherRepository.GetIndependentQuestVoucherIdsAsync());

        var responses = new List<VoucherDto>(vouchers.Count);
        foreach (var voucher in vouchers)
        {
            var dto = MapToDto(voucher);
            dto.CampaignId = await ResolveCampaignIdAsync(voucher);
            dto.IsIndependentQuest = independentVoucherIds.Contains(voucher.VoucherId);
            responses.Add(dto);
        }

        return responses;
    }

    public async Task<VoucherDto> UpdateVoucherAsync(int voucherId, UpdateVoucherDto updateDto, int userId)
    {
        var voucher = await _voucherRepository.GetByIdAsync(voucherId)
            ?? throw new DomainExceptions($"Không tìm thấy phiếu giảm giá với mã {voucherId}");

        if (!string.IsNullOrWhiteSpace(updateDto.Name))
        {
            voucher.Name = updateDto.Name;
        }

        if (updateDto.Description != null)
        {
            voucher.Description = updateDto.Description;
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Type))
        {
            voucher.Type = VoucherRules.NormalizeDiscountType(updateDto.Type);
        }

        if (updateDto.DiscountValue.HasValue)
        {
            voucher.DiscountValue = updateDto.DiscountValue.Value;
        }

        if (updateDto.MinAmountRequired.HasValue)
        {
            voucher.MinAmountRequired = updateDto.MinAmountRequired.Value;
        }

        if (updateDto.MaxDiscountValue.HasValue)
        {
            voucher.MaxDiscountValue = updateDto.MaxDiscountValue.Value;
        }

        if (updateDto.StartDate.HasValue)
        {
            voucher.StartDate = updateDto.StartDate.Value;
        }

        if (updateDto.EndDate.HasValue)
        {
            voucher.EndDate = updateDto.EndDate.Value;
        }

        ValidateDateRange(voucher.StartDate, voucher.EndDate);
        VoucherRules.ValidateDiscountValue(voucher.Type, voucher.DiscountValue);


        if (!string.IsNullOrWhiteSpace(updateDto.VoucherCode) && !string.Equals(voucher.VoucherCode, updateDto.VoucherCode, StringComparison.OrdinalIgnoreCase))
        {
            var existed = await _voucherRepository.GetByCodeAsync(updateDto.VoucherCode);
            if (existed != null && existed.VoucherId != voucher.VoucherId)
            {
                throw new DomainExceptions("Mã phiếu giảm giá đã tồn tại");
            }

            voucher.VoucherCode = updateDto.VoucherCode;
        }

        if (updateDto.RedeemPoint.HasValue)
        {
            voucher.RedeemPoint = updateDto.RedeemPoint.Value;
        }

        if (updateDto.Quantity.HasValue)
        {
            voucher.Quantity = updateDto.Quantity.Value;
        }

        if (updateDto.UsedQuantity.HasValue)
        {
            voucher.UsedQuantity = updateDto.UsedQuantity.Value;
        }

        if (voucher.Quantity >= 0 && voucher.UsedQuantity > voucher.Quantity)
        {
            throw new DomainExceptions("Số lượng đã sử dụng không được lớn hơn tổng số lượng");
        }

        if (updateDto.IsActive.HasValue)
        {
            voucher.IsActive = updateDto.IsActive.Value;
        }

        await _voucherRepository.UpdateAsync(voucher);
        return MapToDto(voucher);
    }

    public async Task<ClaimVoucherResponseDto> ClaimVoucherAsync(int voucherId, int userId)
    {
        var user = await _userRepository.GetUserById(userId)
            ?? throw new DomainExceptions("Không tìm thấy người dùng");

        var voucher = await _voucherRepository.GetByIdAsync(voucherId)
            ?? throw new DomainExceptions("Không tìm thấy phiếu giảm giá");

        var now = DateTime.UtcNow;
        if (!voucher.IsActive)
        {
            throw new DomainExceptions("Phiếu giảm giá không hoạt động");
        }


        VoucherRules.EnsureVoucherIsWithinValidDateRange(voucher, now);

        if (VoucherRules.IsOutOfStock(voucher))
        {
            throw new DomainExceptions("Phiếu giảm giá đã hết");
        }

        VoucherRules.NormalizeDiscountType(voucher.Type);

        var userVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, voucherId);

        // Voucher Hunt Logic: if linked to a campaign, it's free but limited to 1 per user
        bool isCampaignVoucher = voucher.VendorCampaignId.HasValue;

        if (isCampaignVoucher)
        {
            if (userVoucher != null)
            {
                throw new DomainExceptions("Bạn đã nhận phiếu giảm giá của chiến dịch này rồi.");
            }
        }
        else
        {
            if (user.Point < voucher.RedeemPoint)
            {
                throw new DomainExceptions("Không đủ điểm để nhận phiếu giảm giá này");
            }

            user.Point -= voucher.RedeemPoint;
            await _userRepository.UpdateAsync(user);
        }

        voucher.UsedQuantity += 1;
        await _voucherRepository.UpdateAsync(voucher);

        if (userVoucher != null)
        {
            userVoucher.Quantity += 1;
            userVoucher.IsAvailable = true;
            await _userVoucherRepository.UpdateAsync(userVoucher);
        }
        else
        {
            userVoucher = await _userVoucherRepository.CreateAsync(new UserVoucher
            {
                UserId = userId,
                VoucherId = voucherId,
                Quantity = 1,
                IsAvailable = true
            });
        }

        return new ClaimVoucherResponseDto
        {
            UserVoucherId = userVoucher.UserVoucherId,
            VoucherId = voucher.VoucherId,
            VoucherCode = voucher.VoucherCode,
            VoucherName = voucher.Name,
            VoucherType = voucher.Type,
            DiscountValue = voucher.DiscountValue,
            MaxDiscountValue = voucher.MaxDiscountValue,
            Quantity = userVoucher.Quantity,
            RemainingUserPoint = user.Point,
            Remain = VoucherRules.GetRemainingQuantity(voucher)
        };
    }

    public async Task<bool> DeleteVoucherAsync(int voucherId, int userId)
    {
        var exists = await _voucherRepository.ExistsByIdAsync(voucherId);
        if (!exists)
        {
            throw new DomainExceptions($"Không tìm thấy phiếu giảm giá với mã {voucherId}");
        }

        await _voucherRepository.DeleteAsync(voucherId);
        return true;
    }

    public async Task<List<UserVoucherResponseDto>> GetUserVouchersAsync(int userId)
    {
        var userVouchers = await _userVoucherRepository.GetByUserIdAsync(userId);

        var responses = new List<UserVoucherResponseDto>();
        foreach (var uv in userVouchers)
        {
            var campaignId = uv.Voucher == null ? null : await ResolveCampaignIdAsync(uv.Voucher);

            responses.Add(new UserVoucherResponseDto
            {
                UserVoucherId = uv.UserVoucherId,
                VoucherId = uv.VoucherId,
                VoucherCode = uv.Voucher?.VoucherCode ?? string.Empty,
                VoucherName = uv.Voucher?.Name ?? string.Empty,
                Description = uv.Voucher?.Description,
                VoucherType = uv.Voucher?.Type ?? string.Empty,
                DiscountValue = uv.Voucher?.DiscountValue ?? 0m,
                MinAmountRequired = uv.Voucher?.MinAmountRequired,
                MaxDiscountValue = uv.Voucher?.MaxDiscountValue,
                StartDate = uv.Voucher?.StartDate,
                EndDate = uv.Voucher?.EndDate,
                IsActive = uv.Voucher?.IsActive ?? false,
                CampaignId = campaignId,
                Quantity = uv.Quantity,
                IsAvailable = uv.IsAvailable
            });
        }

        return responses;
    }

    public async Task<List<VoucherDto>> GetVouchersByCampaignIdAsync(int campaignId)
    {
        var vouchers = await _voucherRepository.GetByCampaignIdAsync(campaignId);
        return vouchers
            .Select(voucher =>
            {
                var dto = MapToDto(voucher);
                var isMarketplaceVoucher = voucher.VendorCampaignId == null && voucher.RedeemPoint > 0;
                if (!dto.CampaignId.HasValue && !isMarketplaceVoucher)
                {
                    dto.CampaignId = campaignId;
                }

                return dto;
            })
            .ToList();
    }

    public async Task<List<UserVoucherResponseDto>> GetApplicableUserVouchersAsync(int userId, int branchId)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId)
            ?? throw new DomainExceptions("Không tìm thấy chi nhánh");

        var userVouchers = await _userVoucherRepository.GetByUserIdAsync(userId);
        var applicableByVoucherId = new Dictionary<int, UserVoucherResponseDto>();
        var now = DateTime.UtcNow;

        foreach (var uv in userVouchers)
        {
            if (!uv.IsAvailable || uv.Quantity <= 0) continue;

            var voucher = uv.Voucher;
            if (voucher == null) continue;

            if (!voucher.IsActive) continue;
            if (!VoucherRules.IsWithinValidDateRange(voucher, now)) continue;

            var isApplicable = await IsVoucherApplicableToBranchAsync(voucher, branchId, branch.IsSubscribed);
            if (!isApplicable) continue;

            applicableByVoucherId[voucher.VoucherId] = new UserVoucherResponseDto
            {
                UserVoucherId = uv.UserVoucherId,
                VoucherId = uv.VoucherId,
                VoucherCode = voucher.VoucherCode,
                VoucherName = voucher.Name,
                Description = voucher.Description,
                VoucherType = voucher.Type,
                DiscountValue = voucher.DiscountValue,
                MinAmountRequired = voucher.MinAmountRequired,
                MaxDiscountValue = voucher.MaxDiscountValue,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                IsActive = voucher.IsActive,
                CampaignId = voucher.VendorCampaignId,
                Quantity = uv.Quantity,
                IsAvailable = uv.IsAvailable
            };
        }

        // Vendor-created vouchers can be applied directly without claiming.
        var allVouchers = await _voucherRepository.GetAllAsync();
        foreach (var voucher in allVouchers)
        {
            if (!voucher.VendorCampaignId.HasValue || voucher.VendorCampaign == null)
            {
                continue;
            }

            if (!voucher.IsActive || VoucherRules.IsOutOfStock(voucher))
            {
                continue;
            }

            if (!VoucherRules.IsWithinValidDateRange(voucher, now))
            {
                continue;
            }

            var isApplicable = await IsVoucherApplicableToBranchAsync(voucher, branchId, branch.IsSubscribed);
            if (!isApplicable)
            {
                continue;
            }

            if (applicableByVoucherId.ContainsKey(voucher.VoucherId))
            {
                continue;
            }

            applicableByVoucherId[voucher.VoucherId] = new UserVoucherResponseDto
            {
                UserVoucherId = null,
                VoucherId = voucher.VoucherId,
                VoucherCode = voucher.VoucherCode,
                VoucherName = voucher.Name,
                Description = voucher.Description,
                VoucherType = voucher.Type,
                DiscountValue = voucher.DiscountValue,
                MinAmountRequired = voucher.MinAmountRequired,
                MaxDiscountValue = voucher.MaxDiscountValue,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                IsActive = voucher.IsActive,
                CampaignId = voucher.VendorCampaignId,
                Quantity = VoucherRules.GetRemainingQuantity(voucher),
                IsAvailable = true
            };
        }

        return applicableByVoucherId
            .Values
            .OrderByDescending(v => v.UserVoucherId.HasValue)
            .ThenBy(v => v.VoucherId)
            .ToList();
    }

    private async Task<bool> IsVoucherApplicableToBranchAsync(Voucher voucher, int branchId, bool isBranchSubscribed)
    {
        var campaignId = await ResolveCampaignIdAsync(voucher);

        if (campaignId.HasValue)
        {
            var joinInfo = await _branchCampaignRepository.GetByBranchAndCampaignAsync(branchId, campaignId.Value);
            return joinInfo != null && joinInfo.IsActive == true;
        }

        return isBranchSubscribed;
    }

    private async Task<int?> ResolveCampaignIdAsync(Voucher voucher)
    {
        if (voucher.VendorCampaignId.HasValue)
        {
            return voucher.VendorCampaignId.Value;
        }

        return await _voucherRepository.GetSystemCampaignIdAsync(voucher.VoucherId);
    }

    public async Task<List<VoucherDto>> GetMarketplaceVouchersAsync()
    {
        var now = DateTime.UtcNow;
        var vouchers = await _voucherRepository.GetMarketplaceVouchersAsync(now);
        return vouchers.Select(MapToDto).ToList();
    }

    private static void ValidateDateRange(DateTime startDate, DateTime? endDate)
    {
        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new DomainExceptions("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu");
        }
    }

    private static VoucherDto MapToDto(Voucher voucher)
    {
        return new VoucherDto
        {
            VoucherId = voucher.VoucherId,
            Name = voucher.Name,
            Description = voucher.Description,
            Type = voucher.Type,
            DiscountValue = voucher.DiscountValue,
            MinAmountRequired = voucher.MinAmountRequired,
            MaxDiscountValue = voucher.MaxDiscountValue,
            StartDate = voucher.StartDate,
            EndDate = voucher.EndDate,
            IsActive = voucher.IsActive,
            VoucherCode = voucher.VoucherCode,
            RedeemPoint = voucher.RedeemPoint,
            Quantity = voucher.Quantity,
            UsedQuantity = voucher.UsedQuantity,
            CampaignId = voucher.VendorCampaignId,
            Remain = VoucherRules.GetRemainingQuantity(voucher)
        };
    }

    private static CreateVoucherResponseDto MapToCreateResponseDto(Voucher voucher)
    {
        return new CreateVoucherResponseDto
        {
            VoucherId = voucher.VoucherId,
            Name = voucher.Name,
            Description = voucher.Description,
            Type = voucher.Type,
            DiscountValue = voucher.DiscountValue,
            MinAmountRequired = voucher.MinAmountRequired,
            MaxDiscountValue = voucher.MaxDiscountValue,
            StartDate = voucher.StartDate,
            EndDate = voucher.EndDate,
            IsActive = voucher.IsActive,
            VoucherCode = voucher.VoucherCode,
            RedeemPoint = voucher.RedeemPoint,
            Quantity = voucher.Quantity,
            UsedQuantity = voucher.UsedQuantity,
            Remain = VoucherRules.GetRemainingQuantity(voucher)
        };
    }
}
