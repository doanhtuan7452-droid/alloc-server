using AllocServer.Interfaces.WorkspaceMemberProfiles;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AllocServer.Services.WorkspaceMemberProfile_Services
{
    public class ProfileCalculationQueue : IProfileCalculationQueue
    {
        private readonly Channel<int> _queue;

        public int Count => _queue.Reader.Count;

        public ProfileCalculationQueue()
        {
            var options = new BoundedChannelOptions(5000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<int>(options);
        }

        public async ValueTask QueueProfileCalculationAsync(int memberId, CancellationToken cancellationToken = default)
        {
            await _queue.Writer.WriteAsync(memberId, cancellationToken);
        }

        public async ValueTask<List<int>> DequeueBatchAsync(int maxBatchSize, CancellationToken cancellationToken)
        {
            var batch = new List<int>();

            await _queue.Reader.WaitToReadAsync(cancellationToken);

            while (batch.Count < maxBatchSize && _queue.Reader.TryRead(out var item))
            {
                batch.Add(item);
            }

            return batch;
        }
    }
}
