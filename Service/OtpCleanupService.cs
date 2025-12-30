using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Repository;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{
    public class OtpCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public OtpCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<IOtpVerifyRepository>();
                    var deleted = await repo.DeleteExpiredOtpsAsync();
                    Console.WriteLine($"[OTP Cleanup] Deleted {deleted} expired OTPs at {DateTime.UtcNow}");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // run every 1 hour
            }
        }
    }
}
