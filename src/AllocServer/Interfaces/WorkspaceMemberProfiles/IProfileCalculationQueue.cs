using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Interfaces.WorkspaceMemberProfiles
{
    public interface IProfileCalculationQueue
    {
        int Count { get; }
        ValueTask QueueProfileCalculationAsync(int memberId, CancellationToken cancellationToken = default);
        
        ValueTask<List<int>> DequeueBatchAsync(int maxBatchSize, CancellationToken cancellationToken);
    }
}
