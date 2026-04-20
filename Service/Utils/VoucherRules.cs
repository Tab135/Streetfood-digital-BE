using BO.Entities;
using BO.Exceptions;

namespace Service.Utils;

public static class VoucherRules
{
    public const string DiscountTypeAmount = "AMOUNT";
    public const string DiscountTypePercent = "PERCENT";

    public static string NormalizeDiscountType(string discountType)
    {
        if (string.IsNullOrWhiteSpace(discountType))
        {
            throw new DomainExceptions("Voucher type is required");
        }

        var normalized = discountType.Trim().ToUpperInvariant();
        return normalized switch
        {
            "PERCENT" => DiscountTypePercent,
            "PERCENTAGE" => DiscountTypePercent,
            "AMOUNT" => DiscountTypeAmount,
            "FIXED" => DiscountTypeAmount,
            _ => throw new DomainExceptions("Voucher type must be AMOUNT or PERCENT")
        };
    }

    public static bool IsPercentageType(string discountType)
    {
        return string.Equals(NormalizeDiscountType(discountType), DiscountTypePercent, StringComparison.OrdinalIgnoreCase);
    }

    public static decimal CalculateDiscountAmount(decimal amount, Voucher voucher)
    {
        decimal calculatedDiscount;

        if (IsPercentageType(voucher.Type))
        {
            calculatedDiscount = amount * voucher.DiscountValue / 100m;
        }
        else
        {
            calculatedDiscount = voucher.DiscountValue;
        }

        if (voucher.MaxDiscountValue.HasValue && calculatedDiscount > voucher.MaxDiscountValue.Value)
        {
            calculatedDiscount = voucher.MaxDiscountValue.Value;
        }

        if (calculatedDiscount < 0)
        {
            calculatedDiscount = 0;
        }

        return Math.Min(calculatedDiscount, amount);
    }

    public static bool IsWithinValidDateRange(Voucher voucher, DateTime now)
    {
        return now >= voucher.StartDate
            && (!voucher.EndDate.HasValue || now <= voucher.EndDate.Value);
    }

    public static bool HasUnlimitedQuantity(Voucher voucher)
    {
        return voucher.Quantity < 0;
    }

    public static bool HasRemainingQuantity(Voucher voucher)
    {
        return HasUnlimitedQuantity(voucher) || voucher.UsedQuantity < voucher.Quantity;
    }

    public static bool IsOutOfStock(Voucher voucher)
    {
        return !HasRemainingQuantity(voucher);
    }

    public static int GetRemainingQuantity(Voucher voucher)
    {
        return HasUnlimitedQuantity(voucher)
            ? voucher.Quantity
            : Math.Max(voucher.Quantity - voucher.UsedQuantity, 0);
    }

    public static void EnsureVoucherIsWithinValidDateRange(Voucher voucher, DateTime now)
    {
        if (!IsWithinValidDateRange(voucher, now))
        {
            throw new DomainExceptions("Voucher is out of valid time range");
        }
    }

    public static void ValidateDiscountValue(string discountType, decimal discountValue)
    {
        var normalizedType = NormalizeDiscountType(discountType);
        if (normalizedType == DiscountTypePercent && (discountValue <= 0 || discountValue > 100))
        {
            throw new DomainExceptions("Percentage discount value must be greater than 0 and less than or equal to 100");
        }
    }
}
