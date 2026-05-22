using AllocServer.DTOs.Notifications;

namespace AllocServer.Interfaces.Notifications
{
    public interface INotificationQueue
    {
        ValueTask QueueNotificationAsync(NotificationDispatchMessage message, CancellationToken cancellationToken = default);
        ValueTask<NotificationDispatchMessage> DequeueAsync(CancellationToken cancellationToken);
    }
}
