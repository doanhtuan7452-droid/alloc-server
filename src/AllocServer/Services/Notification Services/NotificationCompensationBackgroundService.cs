using AllocServer.Data;
using AllocServer.DTOs.Notifications;
using AllocServer.Interfaces.Notifications;
using AllocServer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Services.Notification_Services
{
    public class NotificationCompensationBackgroundService : BackgroundService
    {
        private readonly INotificationCompensationQueue _compensationQueue;
        private readonly INotificationQueue _notificationQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationCompensationBackgroundService> _logger;

        public NotificationCompensationBackgroundService(
            INotificationCompensationQueue compensationQueue,
            INotificationQueue notificationQueue,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationCompensationBackgroundService> logger)
        {
            _compensationQueue = compensationQueue;
            _notificationQueue = notificationQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Compensation Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var item = await _compensationQueue.DequeueAsync(stoppingToken);
                    await ProcessCompensationWithRetryAsync(item, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing notification compensation.");
                }
            }

            _logger.LogInformation("Notification Compensation Background Service is stopping.");
        }

        private async Task ProcessCompensationWithRetryAsync(NotificationCompensationItem item, CancellationToken stoppingToken)
        {
            int maxRetries = 5;
            int delayMs = 1000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var notification = new Notification
                    {
                        RecipientID = item.RecipientID,
                        ActorID = item.ActorID,
                        NotificationType = item.NotificationType,
                        Title = item.Title,
                        Message = item.Message,
                        ReferenceType = item.ReferenceType,
                        ReferenceID = item.ReferenceID,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        MetadataJson = item.MetadataJson
                    };

                    dbContext.Notifications.Add(notification);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    var dto = new NotificationDTO
                    {
                        NotificationID = notification.NotificationID,
                        NotificationType = notification.NotificationType,
                        Title = notification.Title,
                        Message = notification.Message,
                        ReferenceType = notification.ReferenceType,
                        ReferenceID = notification.ReferenceID,
                        IsRead = notification.IsRead,
                        CreatedAt = notification.CreatedAt,
                        MetadataJson = notification.MetadataJson
                    };

                    await _notificationQueue.QueueNotificationAsync(new NotificationDispatchMessage
                    {
                        RecipientID = notification.RecipientID,
                        Payload = dto
                    }, stoppingToken);

                    _logger.LogInformation("Successfully processed notification compensation for RecipientID {RecipientID} on attempt {Attempt}", item.RecipientID, attempt);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process notification compensation for RecipientID {RecipientID} on attempt {Attempt}. Retrying in {Delay}ms.", item.RecipientID, attempt, delayMs);
                    
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "Max retries reached. Dropping compensation item for RecipientID {RecipientID}.", item.RecipientID);
                        return;
                    }

                    await Task.Delay(delayMs, stoppingToken);
                    delayMs *= 2; // Exponential backoff
                }
            }
        }
    }
}
