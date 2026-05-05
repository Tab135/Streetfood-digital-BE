using BO.Entities;

namespace BO.DTO.Order;

public class AdminOrderUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
}

public class AdminOrderBranchDto
{
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int? VendorId { get; set; }
    public string? VendorName { get; set; }
}

public class AdminOrderPaymentDto
{
    public int Id { get; set; }
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string? Status { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? TransactionCode { get; set; }
}

public class AdminOrderVoucherDto
{
    public int VoucherId { get; set; }
    public string? VoucherCode { get; set; }
    public string? VoucherName { get; set; }
}

public class AdminOrderResponseDto
{
    public int OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public string? Table { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public bool IsTakeAway { get; set; }
    public int? OrderXP { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string MoneyLocation { get; set; } = string.Empty;

    public AdminOrderUserDto User { get; set; } = new();
    public AdminOrderBranchDto Branch { get; set; } = new();
    public AdminOrderPaymentDto? Payment { get; set; }
    public AdminOrderVoucherDto? AppliedVoucher { get; set; }
    public List<OrderDishResponseDto> Items { get; set; } = new();
}
