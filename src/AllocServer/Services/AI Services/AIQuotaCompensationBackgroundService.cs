using System;
using System.Threading;
using System.Threading.Tasks;
using AllocServer.Data;
using AllocServer.Interfaces.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllocServer.Services.AI_Services
{
    public class AIQuotaCompensationBackgroundService : BackgroundService
    {
        private readonly IAIQuotaCompensationQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AIQuotaCompensationBackgroundService> _logger;

        public AIQuotaCompensationBackgroundService(
            IAIQuotaCompensationQueue queue,
            IServiceProvider serviceProvider,
            ILogger<AIQuotaCompensationBackgroundService> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var (workspaceId, billingMonth) = await _queue.DequeueAsync(stoppingToken);
                    await ProcessCompensationWithRetryAsync(workspaceId, billingMonth, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Loi xay ra trong vong lap AIQuotaCompensationBackgroundService.");
                }
            }
        }

        private async Task ProcessCompensationWithRetryAsync(int workspaceId, DateOnly billingMonth, CancellationToken token)
        {
            int maxRetryAttempts = 5;
            int delaySeconds = 5;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE WorkspaceMonthlyUsages
SET AIQueryCount = CASE WHEN ISNULL(AIQueryCount, 0) > 0 THEN AIQueryCount - 1 ELSE 0 END,
    UpdatedAt = SYSUTCDATETIME()
WHERE WorkspaceID = {workspaceId}
  AND BillingMonth = {billingMonth}");

                    _logger.LogInformation("Da hoan tra quota thanh cong cho Workspace {WorkspaceId} (Lan thu {Attempt}).", workspaceId, attempt);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Thu lai lan {Attempt} hoan tra quota cho Workspace {WorkspaceId} that bai.", attempt, workspaceId);
                    if (attempt == maxRetryAttempts)
                    {
                        _logger.LogCritical(ex, "CRITICAL: Khong the hoan tra quota cho Workspace {WorkspaceId} sau {MaxAttempts} lan thu. Can su can thiep thu cong.", workspaceId, maxRetryAttempts);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
                        delaySeconds *= 2; // Exponential backoff
                    }
                }
            }
        }
    }
}
