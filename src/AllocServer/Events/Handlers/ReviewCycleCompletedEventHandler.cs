using AllocServer.Data;
using AllocServer.Events;
using AllocServer.Events.DomainEvents;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Events.Handlers
{
    public class ReviewCycleCompletedEventHandler : IEventHandler<ReviewCycleCompletedEvent>
    {
        private readonly ApplicationDbContext _context;
        private readonly IProfileCalculationQueue _queue;

        public ReviewCycleCompletedEventHandler(ApplicationDbContext context, IProfileCalculationQueue queue)
        {
            _context = context;
            _queue = queue;
        }

        public async Task HandleAsync(ReviewCycleCompletedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var revieweeIds = await _context.MemberEvaluations
                .AsNoTracking()
                .Where(e => e.CycleID == domainEvent.CycleID)
                .Select(e => e.RevieweeID)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var memberId in revieweeIds)
            {
                await _queue.QueueProfileCalculationAsync(memberId, cancellationToken);
            }
        }
    }
}
