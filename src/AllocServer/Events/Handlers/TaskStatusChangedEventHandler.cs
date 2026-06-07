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
    public class TaskStatusChangedEventHandler : IEventHandler<TaskStatusChangedEvent>
    {
        private readonly ApplicationDbContext _context;
        private readonly IProfileCalculationQueue _queue;

        public TaskStatusChangedEventHandler(ApplicationDbContext context, IProfileCalculationQueue queue)
        {
            _context = context;
            _queue = queue;
        }

        public async Task HandleAsync(TaskStatusChangedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            if (domainEvent.NewStatus == "Done" || domainEvent.OldStatus == "Done")
            {
                var assignees = await _context.TaskAssignees
                    .AsNoTracking()
                    .Where(ta => ta.TaskID == domainEvent.TaskID && ta.AssigneeType == "Assignee")
                    .Select(ta => ta.WorkspaceMemberID)
                    .ToListAsync(cancellationToken);

                foreach (var memberId in assignees)
                {
                    await _queue.QueueProfileCalculationAsync(memberId, cancellationToken);
                }
            }
        }
    }
}
