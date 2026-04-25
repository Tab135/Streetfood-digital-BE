using BO.Entities;
using DAL;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDAO _paymentDAO;

        public PaymentRepository(PaymentDAO paymentDAO)
        {
            _paymentDAO = paymentDAO;
        }

        public async Task<Payment> CreatePayment(int userId, long orderCode, int? branchId,
            int amount, string description, string? checkoutUrl = null, int? orderId = null, int? branchCampaignId = null)
        {
            return await _paymentDAO.CreatePayment(userId, orderCode, branchId, amount, description, checkoutUrl, orderId, branchCampaignId);
        }

        public async Task<Payment?> GetPaymentByOrderCode(long orderCode)
        {
            return await _paymentDAO.GetPaymentByOrderCode(orderCode);
        }

        public async Task<Payment?> GetLatestPaymentByOrderId(int orderId)
        {
            return await _paymentDAO.GetLatestPaymentByOrderId(orderId);
        }

        public async Task<Payment?> GetPaymentById(int id)
        {
            return await _paymentDAO.GetPaymentById(id);
        }

        public async Task<List<Payment>> GetUserPayments(int userId, string? status = null)
        {
            return await _paymentDAO.GetUserPayments(userId, status);
        }

        public async Task<List<Payment>> GetAllPayouts(int pageNumber = 1, int pageSize = 10)
        {
            return await _paymentDAO.GetAllPayouts(pageNumber, pageSize);
        }

        public async Task<int> GetTotalPayoutsCount()
        {
            return await _paymentDAO.GetTotalPayoutsCount();
        }

        public async Task<Payment> UpdatePaymentStatus(long orderCode, string status,
            string? transactionCode = null, string? paymentLinkId = null, string? paymentMethod = null)
        {
            return await _paymentDAO.UpdatePaymentStatus(orderCode, status, transactionCode, paymentLinkId, paymentMethod);
        }

        public async Task<bool> OrderCodeExists(long orderCode)
        {
            return await _paymentDAO.OrderCodeExists(orderCode);
        }

        public async Task<Payment?> UpdatePaymentFromWebhook(
            long orderCode,
            string status,
            string? transactionCode,
            DateTime? paidAt,
            string? paymentMethod)
        {
            return await _paymentDAO.UpdatePaymentFromWebhook(orderCode, status, transactionCode, paidAt, paymentMethod);
        }

        public async Task UpdatePaymentWithPayOSDetails(
            long orderCode,
            string status,
            string? paymentLinkId,
            string? checkoutUrl,
            string? bin = null,
            string? accountNumber = null,
            string? accountName = null)
        {
            await _paymentDAO.UpdatePaymentWithPayOSDetails(orderCode, status, paymentLinkId, checkoutUrl, bin, accountNumber, accountName);
        }
    }
}