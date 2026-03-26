using BO.DTO.Payments;
using BO.Entities;
using PayOS.Models.Webhooks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.PaymentsService
{
    public interface IPaymentService
    {
        /// <summary>
        /// Creates a PayOS checkout link for the 20,000 VND vendor subscription fee.
        /// The branch must have been approved by a moderator (RegisterVendorStatus = Accept).
        /// </summary>
        Task<PaymentLinkResult> CreatePaymentLink(int userId, int branchId);
        Task<PaymentLinkResult> CreateOrderPaymentLink(int userId, int orderId);
        Task<PaymentLinkResult> CreateCampaignPaymentLink(int userId, int branchId, int branchCampaignId);
        // Vendor joins a system campaign for all eligible branches in one bill
        Task<PaymentLinkResult> CreateVendorSystemCampaignPaymentLink(
            int userId,
            int campaignId,
            int vendorId,
            List<int> pendingBranchCampaignIds);

        Task<Payment?> GetPaymentByOrderCode(long orderCode);

        Task<List<Payment>> GetUserPaymentHistory(int userId);

        Task<PaymentStatusResponse> GetPaymentStatus(long orderCode);

        Task<PaymentStatusResponse> ConfirmPaymentFromRedirect(long orderCode, string status, string? transactionId);

        Task<VendorPayoutResponseDto> RequestVendorPayoutAsync(int vendorUserId, VendorPayoutRequestDto request);
        Task<decimal> GetVendorBalanceAsync(int vendorUserId);
        
        Task<VendorPayoutResponseDto> RequestUserPayoutAsync(int userId, VendorPayoutRequestDto request);
        Task<decimal> GetUserBalanceAsync(int userId);

        Task<bool> VerifyPaymentOwnership(long orderCode, int userId);

        Task<bool> RegisterWebhookUrl(string webhookUrl);
    }
}
