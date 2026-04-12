using Microsoft.Extensions.Logging;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Threading.Tasks;

namespace Service
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
        private readonly ILogger<CampaignStatusJob> _logger;

        public CampaignStatusJob(
            ICampaignRepository campaignRepo,
            IBranchCampaignRepository branchCampaignRepo,
            ILogger<CampaignStatusJob> logger)
        {
            _campaignRepo = campaignRepo;
            _branchCampaignRepo = branchCampaignRepo;
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

            var branchCount = await _branchCampaignRepo.CountByCampaignIdAsync(campaignId);
            if (branchCount == 0)
            {
                _logger.LogInformation(
                    "CampaignStatusJob: skip activation for campaign {CampaignId} because no branches have joined yet.",
                    campaignId);
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

            var campaignIdsToActivate = await _campaignRepo.GetCampaignIdsToActivateAsync(now);
            var expiredCampaignIds = await _campaignRepo.GetExpiredCampaignIdsAsync(now);
            var campaignIdsToOpenRegistration = await _campaignRepo.GetCampaignIdsToOpenRegistrationAsync(now);
            var campaignIdsToCloseRegistration = await _campaignRepo.GetCampaignIdsToCloseRegistrationAsync(now);

            if (campaignIdsToActivate.Count == 0
                && expiredCampaignIds.Count == 0
                && campaignIdsToOpenRegistration.Count == 0
                && campaignIdsToCloseRegistration.Count == 0)
            {
                _logger.LogDebug("CampaignStatusJob: no campaign status changes found at {Now}.", now);
                return;
            }

            var activatedCampaigns = 0;
            foreach (var campaignId in campaignIdsToActivate)
            {
                await ActivateCampaignAsync(campaignId);
                activatedCampaigns++;
            }

            var deactivatedCampaigns = 0;
            foreach (var campaignId in expiredCampaignIds)
            {
                await DeactivateCampaignAsync(campaignId);
                deactivatedCampaigns++;
            }

            var openedRegistrations = 0;
            foreach (var campaignId in campaignIdsToOpenRegistration)
            {
                await UpdateRegistrationStatusAsync(campaignId, true, now);
                openedRegistrations++;
            }

            var closedRegistrations = 0;
            foreach (var campaignId in campaignIdsToCloseRegistration)
            {
                await UpdateRegistrationStatusAsync(campaignId, false, now);
                closedRegistrations++;
            }

            _logger.LogInformation(
                "CampaignStatusJob: activated {ActivatedCampaignCount} campaign(s), deactivated {DeactivatedCampaignCount} campaign(s), opened registration for {OpenedRegistrationCount} campaign(s), and closed registration for {ClosedRegistrationCount} campaign(s) at {Now}.",
                activatedCampaigns,
                deactivatedCampaigns,
                openedRegistrations,
                closedRegistrations,
                now);
        }

        private async Task UpdateRegistrationStatusAsync(int campaignId, bool isRegisterable, DateTime now)
        {
            var campaign = await _campaignRepo.GetByIdAsync(campaignId);
            if (campaign == null)
            {
                _logger.LogWarning("CampaignStatusJob: campaign {CampaignId} not found for registration update.", campaignId);
                return;
            }

            if (campaign.CreatedByBranchId != null || campaign.CreatedByVendorId != null)
            {
                return;
            }

            if (campaign.IsRegisterable == isRegisterable)
            {
                return;
            }

            campaign.IsRegisterable = isRegisterable;
            campaign.UpdatedAt = now;
            await _campaignRepo.UpdateAsync(campaign);
        }
    }
}