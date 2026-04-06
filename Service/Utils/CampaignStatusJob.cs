using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Utils
{
    /// <summary>
    /// Hangfire jobs for campaign status automation.
    /// - Scheduled jobs handle precise start/end transitions.
    /// - Recurring reconciliation handles missed runs or historical data drift.
    /// </summary>
    public class CampaignStatusJob : ICampaignStatusJob
    {
        private readonly ICampaignRepository _campaignRepo;
        private readonly IBranchCampaignRepository _branchCampaignRepo;
        private readonly StreetFoodDbContext _db;
        private readonly ILogger<CampaignStatusJob> _logger;

        public CampaignStatusJob(
            ICampaignRepository campaignRepo,
            IBranchCampaignRepository branchCampaignRepo,
            StreetFoodDbContext db,
            ILogger<CampaignStatusJob> logger)
        {
            _campaignRepo = campaignRepo;
            _branchCampaignRepo = branchCampaignRepo;
            _db = db;
            _logger = logger;
        }

        public async Task ActivateCampaignAsync(int campaignId)
        {
            var now = DateTime.UtcNow;
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null)
            {
                _logger.LogWarning("CampaignStatusJob: campaign {CampaignId} not found for activation.", campaignId);
                return;
            }

            // Guard stale jobs if campaign window changed after scheduling.
            if (campaign.StartDate > now || campaign.EndDate < now)
            {
                _logger.LogInformation(
                    "CampaignStatusJob: skip activation for campaign {CampaignId} because window is {StartDate} - {EndDate} at {Now}.",
                    campaignId,
                    campaign.StartDate,
                    campaign.EndDate,
                    now);
                return;
            }

            if (!campaign.IsActive)
            {
                campaign.IsActive = true;
                campaign.UpdatedAt = now;
                await _campaignRepo.UpdateAsync(campaign);
            }

            // For vendor/branch campaigns, branch rows should align to campaign active window.
            if (campaign.CreatedByBranchId != null || campaign.CreatedByVendorId != null)
            {
                await _branchCampaignRepo.SetAllIsActiveForCampaignAsync(campaign.CampaignId, true);
            }

            _logger.LogInformation("CampaignStatusJob: activated campaign {CampaignId} at {Now}.", campaignId, now);
        }

        public async Task DeactivateCampaignAsync(int campaignId)
        {
            var now = DateTime.UtcNow;
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null)
            {
                _logger.LogWarning("CampaignStatusJob: campaign {CampaignId} not found for deactivation.", campaignId);
                return;
            }

            // Guard stale jobs if EndDate was pushed later after scheduling.
            if (campaign.EndDate > now)
            {
                _logger.LogInformation(
                    "CampaignStatusJob: skip deactivation for campaign {CampaignId} because EndDate {EndDate} is later than {Now}.",
                    campaignId,
                    campaign.EndDate,
                    now);
                return;
            }

            if (campaign.IsActive)
            {
                campaign.IsActive = false;
                campaign.UpdatedAt = now;
                await _campaignRepo.UpdateAsync(campaign);
            }

            await _branchCampaignRepo.SetAllIsActiveForCampaignAsync(campaign.CampaignId, false);

            _logger.LogInformation("CampaignStatusJob: deactivated campaign {CampaignId} at {Now}.", campaignId, now);
        }

        public async Task ReconcileCampaignStatusesAsync()
        {
            var now = DateTime.UtcNow;

            var campaignIdsToActivate = await _db.Campaigns
                .Where(c => !c.IsActive && c.StartDate <= now && c.EndDate >= now)
                .Select(c => c.CampaignId)
                .ToListAsync();

            var nonSystemCampaignIdsToActivate = await _db.Campaigns
                .Where(c => campaignIdsToActivate.Contains(c.CampaignId)
                         && (c.CreatedByBranchId != null || c.CreatedByVendorId != null))
                .Select(c => c.CampaignId)
                .ToListAsync();

            var expiredCampaignIds = await _db.Campaigns
                .Where(c => c.IsActive && c.EndDate < now)
                .Select(c => c.CampaignId)
                .ToListAsync();

            if (campaignIdsToActivate.Count == 0 && expiredCampaignIds.Count == 0)
            {
                _logger.LogDebug("CampaignStatusJob: no campaign status changes found at {Now}.", now);
                return;
            }

            var activatedCampaigns = 0;
            var activatedBranchCampaigns = 0;

            if (campaignIdsToActivate.Count > 0)
            {
                activatedCampaigns = await _db.Campaigns
                    .Where(c => campaignIdsToActivate.Contains(c.CampaignId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.IsActive, true)
                        .SetProperty(c => c.UpdatedAt, now));

                if (nonSystemCampaignIdsToActivate.Count > 0)
                {
                    activatedBranchCampaigns = await _db.BranchCampaigns
                        .Where(bc => !bc.IsActive && nonSystemCampaignIdsToActivate.Contains(bc.CampaignId))
                        .ExecuteUpdateAsync(s => s.SetProperty(bc => bc.IsActive, true));
                }
            }

            var deactivatedCampaigns = 0;
            var deactivatedBranchCampaigns = 0;

            if (expiredCampaignIds.Count > 0)
            {
                deactivatedCampaigns = await _db.Campaigns
                    .Where(c => expiredCampaignIds.Contains(c.CampaignId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.IsActive, false)
                        .SetProperty(c => c.UpdatedAt, now));

                deactivatedBranchCampaigns = await _db.BranchCampaigns
                    .Where(bc => bc.IsActive && expiredCampaignIds.Contains(bc.CampaignId))
                    .ExecuteUpdateAsync(s => s.SetProperty(bc => bc.IsActive, false));
            }

            _logger.LogInformation(
                "CampaignStatusJob: activated {ActivatedCampaignCount} campaign(s), activated {ActivatedBranchCampaignCount} branch-campaign row(s), deactivated {DeactivatedCampaignCount} campaign(s), and deactivated {DeactivatedBranchCampaignCount} branch-campaign row(s) at {Now}.",
                activatedCampaigns,
                activatedBranchCampaigns,
                deactivatedCampaigns,
                deactivatedBranchCampaigns,
                now);
        }
    }
}