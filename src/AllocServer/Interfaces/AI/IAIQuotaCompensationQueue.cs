using System;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Interfaces.AI
{
    public interface IAIQuotaCompensationQueue
    {
        int Count { get; }
        void QueueCompensation(int workspaceId, DateOnly billingMonth);
        ValueTask<(int WorkspaceId, DateOnly BillingMonth)> DequeueAsync(CancellationToken cancellationToken);
    }
}
