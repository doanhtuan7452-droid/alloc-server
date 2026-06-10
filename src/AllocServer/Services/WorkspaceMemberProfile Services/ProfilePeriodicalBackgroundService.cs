using AllocServer.Data;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Services.WorkspaceMemberProfile_Services
{
    public class ProfilePeriodicalBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProfilePeriodicalBackgroundService> _logger;
        private int _lastCheckedMonth;

        public ProfilePeriodicalBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ProfilePeriodicalBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _lastCheckedMonth = DateTime.UtcNow.Month;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProfilePeriodicalBackgroundService started.");

            // Startup Catch-up Check
            try
            {
                var now = DateTime.UtcNow;
                var prevMonthDate = now.AddMonths(-1);
                var targetMonthStr = prevMonthDate.ToString("yyyy-MM");

                var lastProcessed = await ReadLastProcessedMonthAsync(stoppingToken);
                if (string.IsNullOrEmpty(lastProcessed) || string.Compare(lastProcessed, targetMonthStr, StringComparison.Ordinal) < 0)
                {
                    _logger.LogInformation("Startup catch-up detected. Previous month {Month} has not been processed. Running calculation...", targetMonthStr);
                    await RunCalculationForMonthAsync(prevMonthDate.Year, prevMonthDate.Month, stoppingToken);
                    await SaveLastProcessedMonthAsync(targetMonthStr, stoppingToken);
                    _logger.LogInformation("Completed startup catch-up recalculation for {Month}.", targetMonthStr);
                }
                else
                {
                    _logger.LogInformation("Previous month {Month} is already processed. No catch-up required.", targetMonthStr);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during startup catch-up check.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check every hour
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                    var currentMonth = DateTime.UtcNow.Month;
                    if (currentMonth != _lastCheckedMonth)
                    {
                        _logger.LogInformation("Month transition detected. Recalculating attendance rates for the previous month.");

                        var prevMonthDate = DateTime.UtcNow.AddMonths(-1);
                        var targetMonthStr = prevMonthDate.ToString("yyyy-MM");

                        await RunCalculationForMonthAsync(prevMonthDate.Year, prevMonthDate.Month, stoppingToken);
                        await SaveLastProcessedMonthAsync(targetMonthStr, stoppingToken);

                        _lastCheckedMonth = currentMonth;
                        _logger.LogInformation("Completed monthly attendance rate recalculations.");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ProfilePeriodicalBackgroundService execution.");
                }
            }

            _logger.LogInformation("ProfilePeriodicalBackgroundService stopped.");
        }

        private async Task RunCalculationForMonthAsync(int year, int month, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var profileService = scope.ServiceProvider.GetRequiredService<IWorkspaceMemberProfileService>();

            var activeMemberIds = await dbContext.WorkspaceMemberProfiles
                .Where(p => !p.IsDeleted)
                .Select(p => p.WorkspaceMemberID)
                .ToListAsync(cancellationToken);

            if (activeMemberIds.Any())
            {
                try
                {
                    // Update all active members in bulk
                    await profileService.RecalculateAttendanceRateForMonthBulkAsync(activeMemberIds, year, month);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to calculate attendance rate for {Year}-{Month} in bulk.", year, month);
                }
            }
        }

        private string GetStateFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "attendance_job_state.json");
        }

        private async Task<string?> ReadLastProcessedMonthAsync(CancellationToken cancellationToken)
        {
            var filePath = GetStateFilePath();
            if (!File.Exists(filePath)) return null;

            try
            {
                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var state = System.Text.Json.JsonSerializer.Deserialize<AttendanceJobState>(json);
                return state?.LastProcessedMonth;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read attendance job state from file.");
                return null;
            }
        }

        private async Task SaveLastProcessedMonthAsync(string monthStr, CancellationToken cancellationToken)
        {
            var filePath = GetStateFilePath();
            try
            {
                var state = new AttendanceJobState { LastProcessedMonth = monthStr };
                var json = System.Text.Json.JsonSerializer.Serialize(state);
                await File.WriteAllTextAsync(filePath, json, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save attendance job state to file.");
            }
        }

        private class AttendanceJobState
        {
            public string? LastProcessedMonth { get; set; }
        }
    }
}
