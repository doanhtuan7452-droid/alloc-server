using AllocServer.Interfaces.WorkspaceMemberProfiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Services.WorkspaceMemberProfile_Services
{
    public class ProfileCalculationDispatcherService : BackgroundService
    {
        private readonly IProfileCalculationQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProfileCalculationDispatcherService> _logger;

        public ProfileCalculationDispatcherService(
            IProfileCalculationQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<ProfileCalculationDispatcherService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProfileCalculationDispatcherService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var memberId = await _queue.DequeueAsync(stoppingToken);

                    _logger.LogInformation("Processing recalculation for member ID: {MemberId}", memberId);

                    using var scope = _scopeFactory.CreateScope();
                    var profileService = scope.ServiceProvider.GetRequiredService<IWorkspaceMemberProfileService>();

                    await profileService.RecalculateProfileScoresAsync(memberId);

                    _logger.LogInformation("Successfully completed recalculation for member ID: {MemberId}", memberId);
                }
                catch (OperationCanceledException)
                {
                    // Stopping token was cancelled
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
