using BO.DTO.Voucher;
using BO.Entities;
using BO.Exceptions;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _voucherRepository;

    public VoucherService(IVoucherRepository voucherRepository)
    {
        _voucherRepository = voucherRepository ?? throw new ArgumentNullException(nameof(voucherRepository));
    }

    public async Task<VoucherDto> CreateVoucherAsync(CreateVoucherDto createDto, int userId)
    {
        ValidateDateRange(createDto.StartDate, createDto.EndDate);

        var existed = await _voucherRepository.GetByCodeAsync(createDto.VoucherCode);
        if (existed != null)
        {
            throw new DomainExceptions("Voucher code already exists");
        }

        var entity = new Voucher
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Type = createDto.Type,
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
            UsedQuantity = 0
        };

        var created = await _voucherRepository.CreateAsync(entity);
        return MapToDto(created);
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
            voucher.Type = updateDto.Type;
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
            UsedQuantity = voucher.UsedQuantity
        };
    }
}
