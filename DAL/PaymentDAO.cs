using BO.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class PaymentDAO
    {
        private readonly StreetFoodDbContext _context;
        private readonly ILogger<PaymentDAO> _logger;

        public PaymentDAO(StreetFoodDbContext context, ILogger<PaymentDAO> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =================================================================
        // CORE METHODS (KEPT)
        // =================================================================

           public async Task<Payment> CreatePayment(int userId, long orderCode, int? branchId,
               int amount, string description, string? checkoutUrl = null, int? orderId = null, int? branchCampaignId = null)
        {
            try
            {
                var payment = new Payment
                {
                    UserId = userId,
                    OrderCode = orderCode,
                    BranchId = branchId,
                    Amount = amount,
                    Description = description,
                    Status = "PENDING",
                    CheckoutUrl = checkoutUrl,
                    OrderId = orderId,
                    BranchCampaignId = branchCampaignId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Payment created: OrderCode={OrderCode}, UserId={UserId}, Amount={Amount}, BranchId={BranchId}",
                    orderCode, userId, amount, branchId);

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<Payment?> GetPaymentByOrderCode(long orderCode)
        {
            return await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode);
        }

        public async Task<Payment?> GetLatestPaymentByOrderId(int orderId)
        {
            return await _context.Payments
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Payment?> GetPaymentById(int id)
        {
            return await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Payment>> GetUserPayments(int userId, string? status = null)
        {
            var query = _context.Payments
                .Where(p => p.UserId == userId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status.ToUpper() == status.ToUpper());
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment> UpdatePaymentStatus(
             long orderCode,
             string status,
             string? transactionCode = null,
             string? paymentLinkId = null,
             string? paymentMethod = null)
        {
            try
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderCode == orderCode);

                if (payment == null)
                {
                    throw new Exception($"Payment not found for OrderCode: {orderCode}");
                }

                var oldStatus = payment.Status;
                payment.Status = status;

                if (status.ToUpper() == "PAID")
                {
                    payment.PaidAt = DateTime.UtcNow;
                }

                if (!string.IsNullOrEmpty(transactionCode))
                {
                    payment.TransactionCode = transactionCode;
                }

                if (!string.IsNullOrEmpty(paymentLinkId))
                {
                    payment.PaymentLinkId = paymentLinkId;
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    payment.PaymentMethod = paymentMethod;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Payment status updated: OrderCode={OrderCode}, OldStatus={OldStatus}, NewStatus={NewStatus}",
                    orderCode, oldStatus, status);

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for OrderCode={OrderCode}", orderCode);
                throw;
            }
        }

        public async Task<Payment?> UpdatePaymentFromWebhook(
            long orderCode,
            string status,
            string? transactionCode,
            DateTime? paidAt,
            string? paymentMethod)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode);

            if (payment == null)
            {
                return null;
            }

            payment.Status = status;
            payment.TransactionCode = transactionCode;
            payment.PaidAt = paidAt;
            payment.PaymentMethod = paymentMethod;

            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task UpdatePaymentWithPayOSDetails(
            long orderCode,
            string status,
            string? paymentLinkId,
            string? checkoutUrl)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode);

            if (payment != null)
            {
                payment.Status = status;
                payment.PaymentLinkId = paymentLinkId;
                payment.CheckoutUrl = checkoutUrl;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> OrderCodeExists(long orderCode)
        {
            return await _context.Payments.AnyAsync(p => p.OrderCode == orderCode);
        }
    }
}
