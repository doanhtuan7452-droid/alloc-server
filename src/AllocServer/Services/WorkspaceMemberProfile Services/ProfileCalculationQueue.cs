using AllocServer.Interfaces.WorkspaceMemberProfiles;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AllocServer.Services.WorkspaceMemberProfile_Services
{
    public class ProfileCalculationQueue : IProfileCalculationQueue
    {
        private readonly Channel<int> _queue;

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

        public async ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
