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

    public class CampaignExpiryService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CampaignExpiryService> _logger;

        public CampaignExpiryService(IServiceScopeFactory scopeFactory, ILogger<CampaignExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CampaignExpiryService started — checking every {Interval}.", CheckInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DeactivateExpiredCampaignsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in CampaignExpiryService.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task DeactivateExpiredCampaignsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetFoodDbContext>();

            var now = DateTime.UtcNow;

            var expiredCampaignIds = await db.Campaigns
                .Where(c => c.IsActive && c.EndDate < now)
                .Select(c => c.CampaignId)
                .ToListAsync(ct);

            if (expiredCampaignIds.Count == 0)
            {
                _logger.LogDebug("CampaignExpiryService: no expired campaigns found at {Now}.", now);
                return;
            }

            var updatedCampaigns = await db.Campaigns
                .Where(c => expiredCampaignIds.Contains(c.CampaignId))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsActive, false)
                    .SetProperty(c => c.UpdatedAt, now), ct);

            var updatedBranchCampaigns = await db.BranchCampaigns
                .Where(bc => bc.IsActive && expiredCampaignIds.Contains(bc.CampaignId))
                .ExecuteUpdateAsync(s => s.SetProperty(bc => bc.IsActive, false), ct);

            _logger.LogInformation(
                "CampaignExpiryService: deactivated {CampaignCount} campaign(s) and {BranchCampaignCount} branch-campaign row(s) at {Now}.",
                updatedCampaigns, updatedBranchCampaigns, now);
        }
    }
}

