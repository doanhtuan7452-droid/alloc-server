using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AllocServer.Services.Notification_Services
{
    public class NotificationCompensationItem
    {
        public int RecipientID { get; set; }
        public int? ActorID { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int ReferenceID { get; set; }
        public string? MetadataJson { get; set; }
    }

    public interface INotificationCompensationQueue
    {
        ValueTask QueueCompensationAsync(NotificationCompensationItem item, CancellationToken cancellationToken = default);
        ValueTask<NotificationCompensationItem> DequeueAsync(CancellationToken cancellationToken);
    }

    public class NotificationCompensationQueue : INotificationCompensationQueue
    {
        private readonly Channel<NotificationCompensationItem> _queue;

        public NotificationCompensationQueue()
        {
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<NotificationCompensationItem>(options);
        }

        public async ValueTask QueueCompensationAsync(NotificationCompensationItem item, CancellationToken cancellationToken = default)
        {
            await _queue.Writer.WriteAsync(item, cancellationToken);
        }

        public async ValueTask<NotificationCompensationItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
