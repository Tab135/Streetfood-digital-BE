using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{
    public class TierResetService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TierResetService> _logger;
        private readonly string _logFile = "last_tier_reset.txt";

        public TierResetService(IServiceScopeFactory scopeFactory, ILogger<TierResetService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TierResetService started - checking for 6-month tier resets.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndResetTiersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in TierResetService.");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CheckAndResetTiersAsync(CancellationToken ct)
        {
            var vnTime = DateTime.UtcNow.AddHours(7);
            bool isResetDay = (vnTime.Month == 1 && vnTime.Day == 1) || (vnTime.Month == 7 && vnTime.Day == 1);
            if (!isResetDay) return;

            string todayString = vnTime.ToString("yyyy-MM-dd");
            string lastResetDate = string.Empty;
            if (File.Exists(_logFile))
                lastResetDate = await File.ReadAllTextAsync(_logFile, ct);

            if (lastResetDate.Trim() == todayString) return;

            _logger.LogInformation("Scheduled Reset Day ({Date}) reached. Resetting tiers...", todayString);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetFoodDbContext>();
            var questProgressService = scope.ServiceProvider.GetRequiredService<IQuestProgressService>();

            var settingGoldXp = await db.Settings.FirstOrDefaultAsync(s => s.Name == "GoldMinXP", ct);
            var settingDiamondXp = await db.Settings.FirstOrDefaultAsync(s => s.Name == "DiamondMinXP", ct);
            int goldXP = settingGoldXp != null && int.TryParse(settingGoldXp.Value, out int gp) ? gp : 3000;
            int diamondXP = settingDiamondXp != null && int.TryParse(settingDiamondXp.Value, out int dp) ? dp : 10000;

            // Branch Tier reset: Diamond (4) -> Gold (3), Gold (3) -> Silver (2)
            var branches = await db.Branches.ToListAsync(ct);
            foreach (var b in branches)
            {
                if (b.TierId == 4) b.TierId = 3;
                else if (b.TierId == 3) b.TierId = 2;
                b.BatchReviewCount = 0;
                b.BatchRatingSum = 0;
            }

            // Customer Tier reset — track who moves to Gold for quest re-grant
            var customers = await db.Users.Where(u => (int)u.Role == 0).ToListAsync(ct);
            var usersResetToGold = new List<int>();

            foreach (var c in customers)
            {
                if (c.TierId == 4) // Diamond -> Gold
                {
                    c.TierId = 3;
                    c.XP = goldXP;
                    usersResetToGold.Add(c.Id);
                }
                else if (c.TierId == 3) // Gold -> Silver
                {
                    c.TierId = 2;
                    c.XP = 0;
                }
                else // Silver and others
                {
                    c.TierId = 2;
                    c.XP = 0;
                }
            }

            await db.SaveChangesAsync(ct);

            // Re-grant Gold tier-up quest for users reset from Diamond -> Gold
            // Each user gets the Gold reward again after the season reset.
            // We delete the old completed UserQuest for the Gold tier-up quest first
            // so idempotency guard doesn't block the re-grant.
            if (usersResetToGold.Count > 0)
            {
                // Find the active Gold (TierId=3) tier-up quest
                var goldTierUpQuest = await db.Quests
                    .Include(q => q.QuestTasks)
                    .FirstOrDefaultAsync(q =>
                        q.IsActive &&
                        !q.RequiresEnrollment &&
                        q.QuestTasks.Any(t => t.Type == BO.Enums.QuestTaskType.TIER_UP && t.TargetValue == 3),
                        ct);

                if (goldTierUpQuest != null)
                {
                    foreach (var userId in usersResetToGold)
                    {
                        using var txScope = await db.Database.BeginTransactionAsync(ct);
                        try
                        {
                            // Delete existing completed UserQuest for this user + Gold quest
                            var oldUserQuest = await db.UserQuests
                                .FirstOrDefaultAsync(uq =>
                                    uq.UserId == userId &&
                                    uq.QuestId == goldTierUpQuest.QuestId &&
                                    uq.Status == "COMPLETED",
                                    ct);
                            if (oldUserQuest != null)
                                db.UserQuests.Remove(oldUserQuest);

                            await db.SaveChangesAsync(ct);
                            await txScope.CommitAsync(ct);

                            // Now re-grant outside the transaction (questProgressService has its own UoW)
                            await questProgressService.HandleTierUpAsync(userId, 3);
                        }
                        catch (Exception ex)
                        {
                            await txScope.RollbackAsync(ct);
                            _logger.LogError(ex, "Failed to re-grant Gold tier-up quest for user {UserId}.", userId);
                        }
                    }
                }
            }

            await File.WriteAllTextAsync(_logFile, todayString, ct);
            _logger.LogInformation("Tiers successfully reset for {Date}.", todayString);
        }
    }
}
