using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{
    /// <summary>
    /// Background service that runs every 6 hours and revokes branch verification
    /// for branches whose 30-day subscription has expired without renewal.
    /// </summary>
    public class SubscriptionExpiryService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpiryService> _logger;

        public SubscriptionExpiryService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SubscriptionExpiryService started — checking every {Interval}.", CheckInterval);

            // Run once at startup, then on the interval
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpireSubscriptionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in SubscriptionExpiryService.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task ExpireSubscriptionsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetFoodDbContext>();

            var now = DateTime.UtcNow;

            // Find branches that have a subscription that has lapsed
            var expiredBranches = await db.Branches
                .Where(b => b.IsSubscribed
                         && b.SubscriptionExpiresAt.HasValue
                         && b.SubscriptionExpiresAt.Value < now)
                .ToListAsync(ct);

            if (expiredBranches.Count == 0)
            {
                _logger.LogDebug("SubscriptionExpiryService: no expired subscriptions found at {Now}.", now);
                return;
            }

            foreach (var branch in expiredBranches)
            {
                // keep verification flag unchanged; just clear subscription
                branch.IsSubscribed = false;
                branch.UpdatedAt = now;

                _logger.LogInformation(
                    "Branch {BranchId} subscription expired at {ExpiresAt} — IsSubscribed set to false.",
                    branch.BranchId, branch.SubscriptionExpiresAt);
            }

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "SubscriptionExpiryService: revoked verification for {Count} expired branch(es).",
                expiredBranches.Count);
        }
    }
}
