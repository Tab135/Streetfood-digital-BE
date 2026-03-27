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
    private readonly IBranchCampaignRepository _branchCampaignRepository;
    private readonly IBranchRepository _branchRepository;

    public VoucherService(
        IVoucherRepository voucherRepository,
        IUserVoucherRepository userVoucherRepository,
        IUserRepository userRepository,
        IBranchCampaignRepository branchCampaignRepository,
        IBranchRepository branchRepository)
    {
        _voucherRepository = voucherRepository ?? throw new ArgumentNullException(nameof(voucherRepository));
        _userVoucherRepository = userVoucherRepository ?? throw new ArgumentNullException(nameof(userVoucherRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _branchCampaignRepository = branchCampaignRepository ?? throw new ArgumentNullException(nameof(branchCampaignRepository));
        _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
    }

    public async Task<CreateVoucherResponseDto> CreateVoucherAsync(CreateVoucherDto createDto, int userId)
    {
        ValidateDateRange(createDto.StartDate, createDto.EndDate);
        var normalizedType = VoucherRules.NormalizeDiscountType(createDto.Type);
        VoucherRules.ValidateDiscountValue(normalizedType, createDto.DiscountValue);

        var existed = await _voucherRepository.GetByCodeAsync(createDto.VoucherCode);
        if (existed != null)
        {
            throw new DomainExceptions("Voucher code already exists");
        }

        var entity = new Voucher
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Type = normalizedType,
            DiscountValue = createDto.DiscountValue,
            MinAmountRequired = createDto.MinAmountRequired,
            MaxDiscountValue = createDto.MaxDiscountValue,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate,
            ExpiredDate = createDto.ExpiredDate,
            IsActive = createDto.IsActive,
            VoucherCode = createDto.VoucherCode,
            RedeemPoint = createDto.RedeemPoint,
            Quantity = createDto.Quantity,
            UsedQuantity = 0,
            CampaignId = createDto.CampaignId
        };

        var created = await _voucherRepository.CreateAsync(entity);
        return MapToCreateResponseDto(created);
    }

    public async Task<VoucherDto?> GetVoucherByIdAsync(int voucherId)
    {
        var voucher = await _voucherRepository.GetByIdAsync(voucherId);
        return voucher == null ? null : MapToDto(voucher);
    }

    public async Task<List<VoucherDto>> GetAllVouchersAsync()
    {
        var vouchers = await _voucherRepository.GetAllAsync();
        return vouchers.Select(MapToDto).ToList();
    }

    public async Task<VoucherDto> UpdateVoucherAsync(int voucherId, UpdateVoucherDto updateDto, int userId)
    {
        var voucher = await _voucherRepository.GetByIdAsync(voucherId)
            ?? throw new DomainExceptions($"Voucher with id {voucherId} not found");

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

        if (updateDto.ExpiredDate.HasValue)
        {
            voucher.ExpiredDate = updateDto.ExpiredDate;
        }

        if (!string.IsNullOrWhiteSpace(updateDto.VoucherCode) && !string.Equals(voucher.VoucherCode, updateDto.VoucherCode, StringComparison.OrdinalIgnoreCase))
        {
            var existed = await _voucherRepository.GetByCodeAsync(updateDto.VoucherCode);
            if (existed != null && existed.VoucherId != voucher.VoucherId)
            {
                throw new DomainExceptions("Voucher code already exists");
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

        if (voucher.UsedQuantity > voucher.Quantity)
        {
            throw new DomainExceptions("Used quantity cannot be greater than quantity");
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
            ?? throw new DomainExceptions("User not found");

        var voucher = await _voucherRepository.GetByIdAsync(voucherId)
            ?? throw new DomainExceptions("Voucher not found");

        var now = DateTime.UtcNow;
        if (!voucher.IsActive)
        {
            throw new DomainExceptions("Voucher is inactive");
        }

        if (now < voucher.StartDate || now > voucher.EndDate)
        {
            throw new DomainExceptions("Voucher is out of valid time range");
        }

        if (voucher.ExpiredDate.HasValue && now > voucher.ExpiredDate.Value)
        {
            throw new DomainExceptions("Voucher has expired");
        }

        if (voucher.UsedQuantity >= voucher.Quantity)
        {
            throw new DomainExceptions("Voucher is out of stock");
        }

        VoucherRules.NormalizeDiscountType(voucher.Type);

        var userVoucher = await _userVoucherRepository.GetByUserAndVoucherAsync(userId, voucherId);

        // Voucher Hunt Logic: if linked to a campaign, it's free but limited to 1 per user
        bool isCampaignVoucher = voucher.CampaignId.HasValue;

        if (isCampaignVoucher)
        {
            if (userVoucher != null)
            {
                throw new DomainExceptions("You have already claimed this campaign voucher.");
            }
        }
        else
        {
            if (user.Point < voucher.RedeemPoint)
            {
                throw new DomainExceptions("Insufficient points to claim this voucher");
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
            Remain = Math.Max(voucher.Quantity - voucher.UsedQuantity, 0)
        };
    }

    public async Task<bool> DeleteVoucherAsync(int voucherId, int userId)
    {
        var exists = await _voucherRepository.ExistsByIdAsync(voucherId);
        if (!exists)
        {
            throw new DomainExceptions($"Voucher with id {voucherId} not found");
        }

        await _voucherRepository.DeleteAsync(voucherId);
        return true;
    }

    public async Task<List<UserVoucherResponseDto>> GetUserVouchersAsync(int userId)
    {
        var userVouchers = await _userVoucherRepository.GetByUserIdAsync(userId);

        return userVouchers.Select(uv => new UserVoucherResponseDto
        {
            UserVoucherId = uv.UserVoucherId,
            VoucherId = uv.VoucherId,
            VoucherCode = uv.Voucher?.VoucherCode ?? string.Empty,
            VoucherName = uv.Voucher?.Name ?? string.Empty,
            VoucherType = uv.Voucher?.Type ?? string.Empty,
            DiscountValue = uv.Voucher?.DiscountValue ?? 0m,
            MaxDiscountValue = uv.Voucher?.MaxDiscountValue,
            Quantity = uv.Quantity
        }).ToList();
    }

    public async Task<List<VoucherDto>> GetVouchersByCampaignIdAsync(int campaignId)
    {
        var vouchers = await _voucherRepository.GetByCampaignIdAsync(campaignId);
        return vouchers.Select(MapToDto).ToList();
    }

    public async Task<List<UserVoucherResponseDto>> GetApplicableUserVouchersAsync(int userId, int branchId)
    {
        var branch = await _branchRepository.GetByIdAsync(branchId)
            ?? throw new DomainExceptions("Branch not found");

        var userVouchers = await _userVoucherRepository.GetByUserIdAsync(userId);
        var applicableVouchers = new List<UserVoucher>();

        foreach (var uv in userVouchers)
        {
            if (!uv.IsAvailable || uv.Quantity <= 0) continue;

            var voucher = uv.Voucher;
            if (voucher == null) continue;

            if (voucher.CampaignId.HasValue)
            {
                var campaign = voucher.Campaign;
                if (campaign == null)
                {
                    continue;
                }

                if (campaign.CreatedByBranchId.HasValue)
                {
                    // Requirement: Voucher created by vendor for that specific branch
                    if (branchId == campaign.CreatedByBranchId.Value)
                    {
                        applicableVouchers.Add(uv);
                    }
                }
                else if (campaign.CreatedByVendorId.HasValue)
                {
                    // Requirement: Vendor-wide campaign (applying to ALL branches of that vendor)
                    if (branch.VendorId == campaign.CreatedByVendorId.Value)
                    {
                        applicableVouchers.Add(uv);
                    }
                }
                else
                {
                    // Requirement: System campaign voucher - only usable at branches that joined
                    var joinInfo = await _branchCampaignRepository.GetByBranchAndCampaignAsync(branchId, campaign.CampaignId);
                    if (joinInfo != null && joinInfo.IsActive == true)
                    {
                        applicableVouchers.Add(uv);
                    }
                }
            }
            else
            {
                // Requirement: System voucher (Marketplace) - usable everywhere with IsSubscribed == true
                if (branch.IsSubscribed)
                {
                    applicableVouchers.Add(uv);
                }
            }
        }

        return applicableVouchers.Select(uv => new UserVoucherResponseDto
        {
            UserVoucherId = uv.UserVoucherId,
            VoucherId = uv.VoucherId,
            VoucherCode = uv.Voucher?.VoucherCode ?? string.Empty,
            VoucherName = uv.Voucher?.Name ?? string.Empty,
            VoucherType = uv.Voucher?.Type ?? string.Empty,
            DiscountValue = uv.Voucher?.DiscountValue ?? 0m,
            MaxDiscountValue = uv.Voucher?.MaxDiscountValue,
            Quantity = uv.Quantity
        }).ToList();
    }

    public async Task<List<VoucherDto>> GetMarketplaceVouchersAsync()
    {
        var now = DateTime.UtcNow;
        var vouchers = await _voucherRepository.GetMarketplaceVouchersAsync(now);
        return vouchers.Select(MapToDto).ToList();
    }

    private static void ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
        {
            throw new DomainExceptions("End date must be greater than or equal to start date");
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
            ExpiredDate = voucher.ExpiredDate,
            IsActive = voucher.IsActive,
            VoucherCode = voucher.VoucherCode,
            RedeemPoint = voucher.RedeemPoint,
            Quantity = voucher.Quantity,
            UsedQuantity = voucher.UsedQuantity,
            CampaignId = voucher.CampaignId,
            Remain = Math.Max(voucher.Quantity - voucher.UsedQuantity, 0)
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
            ExpiredDate = voucher.ExpiredDate,
            IsActive = voucher.IsActive,
            VoucherCode = voucher.VoucherCode,
            RedeemPoint = voucher.RedeemPoint,
            Quantity = voucher.Quantity,
            UsedQuantity = voucher.UsedQuantity,
            Remain = Math.Max(voucher.Quantity - voucher.UsedQuantity, 0)
        };
    }
}
