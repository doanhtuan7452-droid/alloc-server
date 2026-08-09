using AllocServer.DTOs.Notifications;

namespace AllocServer.Interfaces.Notifications
{
    public interface INotificationQueue
    {
        int Count { get; }
        ValueTask QueueNotificationAsync(NotificationDispatchMessage message, CancellationToken cancellationToken = default);
        ValueTask<NotificationDispatchMessage> DequeueAsync(CancellationToken cancellationToken);
    }
}
