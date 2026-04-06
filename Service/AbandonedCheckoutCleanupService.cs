using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Service.Interfaces;

namespace Service
{
    public class AbandonedCheckoutCleanupService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan AbandonmentTimeout = TimeSpan.FromMinutes(10);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AbandonedCheckoutCleanupService> _logger;

        public AbandonedCheckoutCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<AbandonedCheckoutCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "AbandonedCheckoutCleanupService started - cancelling pending checkouts older than {TimeoutMinutes} minute(s) every {IntervalMinutes} minute(s).",
                AbandonmentTimeout.TotalMinutes,
                CheckInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    var cancelledCount = await orderService.CancelAbandonedPendingOrdersAsync(AbandonmentTimeout, stoppingToken);

                    if (cancelledCount > 0)
                    {
                        _logger.LogInformation(
                            "AbandonedCheckoutCleanupService cancelled {Count} stale pending checkout order(s).",
                            cancelledCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in AbandonedCheckoutCleanupService.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }
    }
}
