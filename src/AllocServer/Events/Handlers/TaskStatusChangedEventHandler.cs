using AllocServer.Data;
using AllocServer.Events;
using AllocServer.Events.DomainEvents;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using AllocServer.Interfaces.Notifications;
using AllocServer.Models;
using AllocServer.DTOs.Notifications;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Events.Handlers
{
    public class TaskStatusChangedEventHandler : IEventHandler<TaskStatusChangedEvent>
    {
        private readonly ApplicationDbContext _context;
        private readonly IProfileCalculationQueue _profileQueue;
        private readonly INotificationQueue _notificationQueue;

        public TaskStatusChangedEventHandler(
            ApplicationDbContext context, 
            IProfileCalculationQueue profileQueue,
            INotificationQueue notificationQueue)
        {
            _context = context;
            _profileQueue = profileQueue;
            _notificationQueue = notificationQueue;
        }

        public async Task HandleAsync(TaskStatusChangedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            // 1. Recalculate profiles if needed
            if (domainEvent.NewStatus == "Done" || domainEvent.OldStatus == "Done")
            {
                var assignees = await _context.TaskAssignees
                    .AsNoTracking()
                    .Where(ta => ta.TaskID == domainEvent.TaskID && ta.AssigneeType == "Assignee")
                    .Select(ta => ta.WorkspaceMemberID)
                    .ToListAsync(cancellationToken);

                foreach (var memberId in assignees)
                {
                    await _profileQueue.QueueProfileCalculationAsync(memberId, cancellationToken);
                }
            }

            // 2. Send Notifications
            var taskInfo = await _context.ProjectTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TaskID == domainEvent.TaskID, cancellationToken);
            if (taskInfo == null) return;

            var taskMembers = await _context.TaskAssignees
                .Where(ta => ta.TaskID == domainEvent.TaskID && ta.WorkspaceMember.Status == "Active")
                .Select(ta => ta.WorkspaceMemberID)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var memberId in taskMembers)
            {
                var notification = new Notification
                {
                    RecipientID = memberId,
                    NotificationType = "TaskStatusChanged",
                    Title = "Task Status Updated",
                    Message = $"Task '{taskInfo.TaskName}' changed status from '{domainEvent.OldStatus}' to '{domainEvent.NewStatus}'.",
                    ReferenceType = "Task",
                    ReferenceID = domainEvent.TaskID,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(cancellationToken);

                var dto = new NotificationDTO
                {
                    NotificationID = notification.NotificationID,
                    NotificationType = notification.NotificationType,
                    Title = notification.Title,
                    Message = notification.Message,
                    ReferenceType = notification.ReferenceType,
                    ReferenceID = notification.ReferenceID,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };

                await _notificationQueue.QueueNotificationAsync(new NotificationDispatchMessage
                {
                    RecipientID = memberId,
                    Payload = dto
                }, cancellationToken);
            }
        }
    }
}
