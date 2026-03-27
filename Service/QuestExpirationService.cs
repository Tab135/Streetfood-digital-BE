using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{
    /// <summary>
    /// Background service that runs every hour and marks IN_PROGRESS campaign quest
    /// enrollments as EXPIRED when the parent campaign's EndDate has passed.
    /// Standalone quests have no expiry.
    /// </summary>
    public class QuestExpirationService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QuestExpirationService> _logger;

        public QuestExpirationService(
            IServiceScopeFactory scopeFactory,
            ILogger<QuestExpirationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("QuestExpirationService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpireQuestsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in QuestExpirationService.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task ExpireQuestsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var userQuestRepo = scope.ServiceProvider.GetRequiredService<IUserQuestRepository>();

            var expiredUserQuests = await userQuestRepo.GetExpiredQuestsAsync();

            if (expiredUserQuests.Count == 0) return;

            _logger.LogInformation("Expiring {Count} campaign quest enrollment(s).", expiredUserQuests.Count);

            foreach (var uq in expiredUserQuests)
            {
                uq.Status = "EXPIRED";
                await userQuestRepo.UpdateUserQuestAsync(uq);
            }
        }
    }
}
