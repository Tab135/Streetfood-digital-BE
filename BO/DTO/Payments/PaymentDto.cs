using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BO.DTO.Payments
{
    public class CreatePaymentLinkDto
    {
        /// <summary>The branch ID to pay subscription for.</summary>
        public int BranchId { get; set; }
    }

    public class CreateOrderPaymentLinkDto
    {
        public int OrderId { get; set; }
    }

    public class ConfirmPaymentDto
    {
        public long OrderCode { get; set; }
        public string? Status { get; set; }
        public string? Code { get; set; }
        public string? TransactionId { get; set; }
    }

    public class PaymentLinkResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? PaymentUrl { get; set; }
        public long? OrderCode { get; set; }
        public string? PaymentLinkId { get; set; }
        public bool RequiresConfirmation { get; set; }
    }

    public class PaymentStatusResponse
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? TransactionCode { get; set; }
    }

    public class VendorPayoutRequestDto
    {
        [Range(1000, int.MaxValue, ErrorMessage = "Amount must be at least 1000")]
        public int Amount { get; set; }

        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string ToBin { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string ToAccountNumber { get; set; } = string.Empty;

        public List<string>? Category { get; set; }
    }

    public class VendorPayoutResponseDto
    {
        public string ReferenceId { get; set; } = string.Empty;
        public string? PayoutId { get; set; }
        public string? ApprovalState { get; set; }
        public decimal CurrentVendorBalance { get; set; }
    }
}
