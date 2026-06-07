using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Interfaces.WorkspaceMemberProfiles
{
    public interface IProfileCalculationQueue
    {
        ValueTask QueueProfileCalculationAsync(int memberId, CancellationToken cancellationToken = default);
        
        ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
    }
}
