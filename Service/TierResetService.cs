using DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{
    public class TierResetService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TierResetService> _logger;
        private readonly string _logFile = "last_tier_reset.txt"; // Stored in the project root output

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

                // Check roughly every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CheckAndResetTiersAsync(CancellationToken ct)
        {
            // Calculate Vietnam time (UTC+7)
            var nowOriginal = DateTime.UtcNow;
            var vnTime = nowOriginal.AddHours(7);
            
            // Check if today is either January 1st or July 1st
            bool isResetDay = (vnTime.Month == 1 && vnTime.Day == 1) || (vnTime.Month == 7 && vnTime.Day == 1);
            
            if (!isResetDay)
            {
                return;
            }

            string todayString = vnTime.ToString("yyyy-MM-dd");
            string lastResetDate = string.Empty;
            
            if (File.Exists(_logFile))
            {
                lastResetDate = await File.ReadAllTextAsync(_logFile, ct);
            }

            if (lastResetDate.Trim() != todayString)
            {
                _logger.LogInformation("Scheduled Reset Day ({Date}) reached. Resetting all Branch Tiers...", todayString);
                
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<StreetFoodDbContext>();
                
                // Get configured XP settings
                var settingGoldXp = await db.Settings.FirstOrDefaultAsync(s => s.Name == "GoldMinXP", ct);
                var settingDiamondXp = await db.Settings.FirstOrDefaultAsync(s => s.Name == "DiamondMinXP", ct);
                
                int goldXP = settingGoldXp != null && int.TryParse(settingGoldXp.Value, out int gp) ? gp : 3000;
                int diamondXP = settingDiamondXp != null && int.TryParse(settingDiamondXp.Value, out int dp) ? dp : 10000;

                // Branch Tier reset: Hạ 1 bậc cho Diamond (4 -> 3) và Gold (3 -> 2).
                // Các tier nhỏ hơn (Silver = 2, Warning = 1) giữ nguyên.
                var branches = await db.Branches.ToListAsync(ct);
                foreach(var b in branches)
                {
                    if (b.TierId == 4) // Diamond -> Gold
                    {
                        b.TierId = 3;
                    }
                    else if (b.TierId == 3) // Gold -> Silver
                    {
                        b.TierId = 2;
                    }
                    else if (b.TierId == 2) // Silver -> retains Silver
                    {
                        b.TierId = 2;
                    }
                    // Bắt đầu tính lại đếm 20 feedback tiếp theo
                    b.BatchReviewCount = 0;
                    b.BatchRatingSum = 0;
                }

                // Customer Tier reset
                // Diamond (4) -> Gold (3)
                // Gold (3) -> Silver (2)
                // Silver (2) -> Silver (2)
                var customers = await db.Users.Where(u => (int)u.Role == 0).ToListAsync(ct); // Role.User = 0
                foreach(var c in customers)
                {
                    if (c.TierId == 4) // Diamond
                    {
                        c.TierId = 3;
                        c.XP = goldXP; // Set current XP to the base of Gold
                    }
                    else if (c.TierId == 3) // Gold
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


                await File.WriteAllTextAsync(_logFile, todayString, ct);
                _logger.LogInformation("Tiers successfully reset for {Date}.", todayString);
            }
        }
    }
}
