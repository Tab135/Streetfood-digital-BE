using BO.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{

    public class SettingService : ISettingService, IHostedService
    {
        // Key = Setting.Name, Value = Setting entity
        private readonly ConcurrentDictionary<string, Setting> _cache = new(StringComparer.OrdinalIgnoreCase);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SettingService> _logger;

        public SettingService(IServiceScopeFactory scopeFactory, ILogger<SettingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ─── IHostedService ───────────────────────────────────────────────────

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SettingService: loading settings from database…");
            await ReloadAsync();
            _logger.LogInformation("SettingService: {Count} setting(s) loaded.", _cache.Count);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        // ─── ISettingService ──────────────────────────────────────────────────

        public string? GetValue(string name)
        {
            return _cache.TryGetValue(name, out var setting) ? setting.Value : null;
        }

        public int GetInt(string name, int defaultValue = 0)
        {
            var raw = GetValue(name);
            return int.TryParse(raw, out var result) ? result : defaultValue;
        }

        public decimal GetDecimal(string name, decimal defaultValue = 0m)
        {
            var raw = GetValue(name);
            return decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
        }

        public async Task UpdateAsync(string name, string newValue)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISettingRepository>();

            var setting = await repo.GetByNameAsync(name)
                ?? throw new KeyNotFoundException($"Setting '{name}' not found.");

            setting.Value = newValue;
            await repo.UpdateAsync(setting);

            // Reflect the change in the in-memory cache immediately
            _cache[name] = setting;

            _logger.LogInformation("Setting '{Name}' updated to '{Value}'.", name, newValue);
        }

        public IReadOnlyList<Setting> GetAll()
            => _cache.Values.ToList().AsReadOnly();

        public async Task ReloadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISettingRepository>();
            var all = await repo.GetAllAsync();

            _cache.Clear();
            foreach (var s in all)
                _cache[s.Name] = s;
        }
    }
}
