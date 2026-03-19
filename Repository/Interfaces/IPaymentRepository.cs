using BO.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment> CreatePayment(int userId, long orderCode, int? branchId,
            int amount, string description, string? checkoutUrl = null, int? orderId = null);
        Task<Payment?> GetPaymentByOrderCode(long orderCode);
        Task<Payment?> GetLatestPaymentByOrderId(int orderId);
        Task<Payment?> GetPaymentById(int id);
        Task<List<Payment>> GetUserPayments(int userId, string? status = null);
        Task<Payment> UpdatePaymentStatus(long orderCode, string status,
            string? transactionCode = null, string? paymentLinkId = null, string? paymentMethod = null);
        Task<bool> OrderCodeExists(long orderCode);
        Task<Payment?> UpdatePaymentFromWebhook(long orderCode, string status,
            string? transactionCode, DateTime? paidAt, string? paymentMethod);
        Task UpdatePaymentWithPayOSDetails(long orderCode, string status,
            string? paymentLinkId, string? checkoutUrl);
    }
}
