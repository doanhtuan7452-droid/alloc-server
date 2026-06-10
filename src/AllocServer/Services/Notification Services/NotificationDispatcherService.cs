using AllocServer.Hubs;
using AllocServer.Interfaces.Notifications;
using Microsoft.AspNetCore.SignalR;
using AllocServer.Data;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Notification_Services
{
    public class NotificationDispatcherService : BackgroundService
    {
        private readonly INotificationQueue _queue;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationDispatcherService> _logger;

        public NotificationDispatcherService(
            INotificationQueue queue,
            IHubContext<NotificationHub> hubContext,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationDispatcherService> logger)
        {
            _queue = queue;
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Dispatcher Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queue.DequeueAsync(stoppingToken);

                    // 1. Push to SignalR
                    var groupName = ConversationHub.BuildUserGroup(message.RecipientID);
                    await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", message.Payload, stoppingToken);

                    // 2. Push to Firebase/APNs
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var pushService = scope.ServiceProvider.GetRequiredService<IFirebasePushService>();

                    var accountId = await dbContext.WorkspaceMembers
                        .AsNoTracking()
                        .Where(m => m.WorkspaceMemberID == message.RecipientID)
                        .Select(m => m.Resource != null ? (int?)m.Resource.AccountID : null)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (accountId.HasValue)
                    {

                        var activeTokens = await dbContext.NotificationDeviceTokens
                            .Where(t => t.AccountID == accountId && t.IsActive)
                            .ToListAsync(stoppingToken);

                        foreach (var token in activeTokens)
                        {
                            var result = await pushService.SendPushNotificationAsync(
                                token.DeviceToken,
                                message.Payload.Title,
                                message.Payload.Message ?? "",
                                message.Payload.ReferenceType,
                                message.Payload.ReferenceID);

                            if (!result.IsSuccess)
                            {
                                token.FailureCount += 1;
                                token.LastError = result.ErrorMessage;

                                if (token.FailureCount >= 3)
                                {
                                    token.IsActive = false;
                                    token.RevokedAt = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                token.FailureCount = 0;
                                token.LastError = null;
                            }
                        }

                        if (activeTokens.Any())
                        {
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }

                    _logger.LogInformation("Dispatched notification {NotificationID} to recipient {RecipientID}", message.Payload.NotificationID, message.RecipientID);
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing notification dispatch.");
                }
            }

            _logger.LogInformation("Notification Dispatcher Service is stopping.");
        }
    }
}
