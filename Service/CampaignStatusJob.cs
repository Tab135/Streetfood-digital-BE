using Microsoft.Extensions.Logging;
using Repository.Interfaces;
using Service.Interfaces;
using BO.Entities;
using System;
using System.Linq;
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
        private readonly IVendorRepository _vendorRepo;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CampaignStatusJob> _logger;

        public CampaignStatusJob(
            ICampaignRepository campaignRepo,
            IBranchCampaignRepository branchCampaignRepo,
            IVendorRepository vendorRepo,
            INotificationService notificationService,
            ILogger<CampaignStatusJob> logger)
        {
            _campaignRepo = campaignRepo;
            _branchCampaignRepo = branchCampaignRepo;
            _vendorRepo = vendorRepo;
            _notificationService = notificationService;
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
            if (campaign.CreatedByVendorId != null)
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

            if (campaign.CreatedByVendorId != null)
            {
                return;
            }

            if (campaign.IsRegisterable == isRegisterable)
            {
                return;
            }

            campaign.IsRegisterable = isRegisterable;
            
            if (!isRegisterable && campaign.ExpectedBranchJoin.HasValue && campaign.ExpectedBranchJoin > 0)
            {
                var branchCount = await _branchCampaignRepo.CountByCampaignIdAsync(campaignId);
                if (branchCount < campaign.ExpectedBranchJoin.Value)
                {
                    _logger.LogWarning("CampaignStatusJob: Campaign {CampaignId} did not meet ExpectedBranchJoin ({Count}/{Expected}). Canceling.", campaignId, branchCount, campaign.ExpectedBranchJoin.Value);
                    campaign.IsActive = false;
                    campaign.EndDate = now;
                    
                    if (campaign.JoinFee > 0)
                    {
                        var refunds = await _vendorRepo.RefundCampaignJoinFeeAsync(campaignId, campaign.JoinFee);
                        var notifyTasks = refunds.Select(r => _notificationService.NotifyAsync(
                            r.UserId,
                            NotificationType.CampaignCancelledRefund,
                            "Chiến dịch bị hủy và hoàn tiền",
                            $"Chiến dịch '{campaign.Name}' đã bị hủy do không đủ số lượng chi nhánh tham gia. Số tiền {r.Amount:N0} VNĐ đã được hoàn vào tài khoản của bạn.",
                            campaign.CampaignId
                        ));
                        await Task.WhenAll(notifyTasks);
                    }
                }
            }

            campaign.UpdatedAt = now;
            await _campaignRepo.UpdateAsync(campaign);
        }
    }
}