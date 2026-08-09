using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AllocServer.Interfaces.AI;

namespace AllocServer.Services.AI_Services
{
    public class AIQuotaCompensationQueue : IAIQuotaCompensationQueue
    {
        private readonly Channel<(int WorkspaceId, DateOnly BillingMonth)> _channel;

        public int Count => _channel.Reader.Count;

        public AIQuotaCompensationQueue()
        {
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateUnbounded<(int WorkspaceId, DateOnly BillingMonth)>(options);
        }

        public void QueueCompensation(int workspaceId, DateOnly billingMonth)
        {
            _channel.Writer.TryWrite((workspaceId, billingMonth));
        }

        public ValueTask<(int WorkspaceId, DateOnly BillingMonth)> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
