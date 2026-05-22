using AllocServer.DTOs.Notifications;
using AllocServer.Interfaces.Notifications;
using System.Threading.Channels;

namespace AllocServer.Services.Notification_Services
{
    public class NotificationQueue : INotificationQueue
    {
        private readonly Channel<NotificationDispatchMessage> _queue;

        public NotificationQueue()
        {
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<NotificationDispatchMessage>(options);
        }

        public async ValueTask QueueNotificationAsync(NotificationDispatchMessage message, CancellationToken cancellationToken = default)
        {
            await _queue.Writer.WriteAsync(message, cancellationToken);
        }

        public async ValueTask<NotificationDispatchMessage> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
