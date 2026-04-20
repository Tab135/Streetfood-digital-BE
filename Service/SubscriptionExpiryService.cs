using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
{
    public class SubscriptionExpiryService : ISubscriptionExpiryJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpiryService> _logger;

        public SubscriptionExpiryService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ExpireBranchSubscriptionAsync(int branchId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetFoodDbContext>();

            var now = DateTime.UtcNow;
            var branch = await db.Branches.FirstOrDefaultAsync(b => b.BranchId == branchId);
            if (branch == null)
            {
                _logger.LogWarning("SubscriptionExpiryService: branch {BranchId} not found for expiration.", branchId);
                return;
            }

            if (!branch.IsSubscribed || !branch.SubscriptionExpiresAt.HasValue)
            {
                return;
            }

            // Guard stale jobs when subscription was renewed after scheduling.
            if (branch.SubscriptionExpiresAt.Value > now)
            {
                return;
            }

            branch.IsSubscribed = false;
            branch.UpdatedAt = now;
            await db.SaveChangesAsync();

            _logger.LogInformation(
                "Branch {BranchId} subscription expired at {ExpiresAt} — IsSubscribed set to false.",
                branch.BranchId,
                branch.SubscriptionExpiresAt);
        }

        public async Task ReconcileExpiredSubscriptionsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetFoodDbContext>();

            var now = DateTime.UtcNow;

            // Find branches that have a subscription that has lapsed
            var expiredBranches = await db.Branches
                .Where(b => b.IsSubscribed
                         && b.SubscriptionExpiresAt.HasValue
                         && b.SubscriptionExpiresAt.Value <= now)
                .ToListAsync();

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

            await db.SaveChangesAsync();

            _logger.LogInformation(
                "SubscriptionExpiryService: deactivated subscription for {Count} expired branch(es).",
                expiredBranches.Count);
        }
    }
}
