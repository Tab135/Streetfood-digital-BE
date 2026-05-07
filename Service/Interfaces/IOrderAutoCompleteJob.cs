using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IOrderAutoCompleteJob
    {
        /// <summary>
        /// Auto-completes an order and settles vendor payment if order is still in Paid status after timeout.
        /// Called via Hangfire after 2-hour delay from when vendor approves the order.
        /// </summary>
        Task AutoCompleteOrderAsync(int orderId);
    }
}
