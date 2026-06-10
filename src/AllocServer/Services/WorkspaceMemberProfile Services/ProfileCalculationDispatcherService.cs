using AllocServer.Configurations;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace AllocServer.Services.WorkspaceMemberProfile_Services
{
    public class ProfileCalculationDispatcherService : BackgroundService
    {
        private readonly IProfileCalculationQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProfileCalculationDispatcherService> _logger;
        private readonly int _batchSize;

        public ProfileCalculationDispatcherService(
            IProfileCalculationQueue queue,
            IServiceScopeFactory scopeFactory,
            IOptions<BackgroundJobSettings> options,
            ILogger<ProfileCalculationDispatcherService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _batchSize = options.Value.ProfileCalculationBatchSize;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProfileCalculationDispatcherService is starting with batch size {BatchSize}.", _batchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var memberIds = await _queue.DequeueBatchAsync(_batchSize, stoppingToken);

                    if (memberIds != null && memberIds.Any())
                    {
                        _logger.LogInformation("Processing recalculation for {Count} members.", memberIds.Count);

                        using var scope = _scopeFactory.CreateScope();
                        var profileService = scope.ServiceProvider.GetRequiredService<IWorkspaceMemberProfileService>();

                        await profileService.RecalculateProfileScoresBulkAsync(memberIds);

                        _logger.LogInformation("Successfully completed recalculation for {Count} members.", memberIds.Count);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing profile recalculation.");
                }
            }

            _logger.LogInformation("ProfileCalculationDispatcherService is stopping.");
        }
    }
}
