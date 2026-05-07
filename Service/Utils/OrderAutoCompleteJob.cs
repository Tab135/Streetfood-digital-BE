using Microsoft.Extensions.Logging;
using Repository.Interfaces;
using Service.Interfaces;
using BO.Entities;
using System;
using System.Threading.Tasks;

namespace Service.Utils
{
    /// <summary>
    /// Hangfire background job for auto-completing paid orders after no-pickup timeout.
    /// Triggered 2 hours after vendor approves order. If order still Paid, settles vendor payment.
    /// </summary>
    public class OrderAutoCompleteJob : IOrderAutoCompleteJob
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderAutoCompleteJob> _logger;

        public OrderAutoCompleteJob(
            IOrderService orderService,
            ILogger<OrderAutoCompleteJob> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        public async Task AutoCompleteOrderAsync(int orderId)
        {
            try
            {
                await _orderService.AutoCompleteUnpickedOrderAsync(orderId);
                _logger.LogInformation(
                    "OrderAutoCompleteJob: auto-completed unpicked order {OrderId} at {Now}",
                    orderId, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "OrderAutoCompleteJob: failed to auto-complete order {OrderId} at {Now}",
                    orderId, DateTime.UtcNow);
            }
        }
    }
}
