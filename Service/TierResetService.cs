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
                
                // Reset Tier => Silver (2), BatchReviewCount => 0, BatchRatingSum => 0
                await db.Branches.ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.TierId, 2)
                    .SetProperty(b => b.BatchReviewCount, 0)
                    .SetProperty(b => b.BatchRatingSum, 0), ct);

                await File.WriteAllTextAsync(_logFile, todayString, ct);
                
                _logger.LogInformation("Branch Tiers successfully reset for {Date}.", todayString);
            }
        }
    }
}
