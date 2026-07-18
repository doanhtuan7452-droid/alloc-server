using AllocServer.Data;
using AllocServer.DTOs.Notifications;
using AllocServer.Events.DomainEvents;
using AllocServer.Interfaces.Notifications;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Events.Handlers
{
    public class TaskDeadlineChangedEventHandler : IEventHandler<TaskDeadlineChangedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationQueue _notificationQueue;

        public TaskDeadlineChangedEventHandler(ApplicationDbContext dbContext, INotificationQueue notificationQueue)
        {
            _dbContext = dbContext;
            _notificationQueue = notificationQueue;
        }

        public async Task HandleAsync(TaskDeadlineChangedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var taskMembers = await _dbContext.TaskAssignees
                .Where(ta => ta.TaskID == domainEvent.TaskID 
                          && ta.WorkspaceMemberID != domainEvent.ChangerMemberID
                          && ta.WorkspaceMember.Status == "Active")
                .Select(ta => ta.WorkspaceMemberID)
                .Distinct()
                .ToListAsync(cancellationToken);

            var changer = await _dbContext.WorkspaceMembers
                .Include(m => m.Resource)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == domainEvent.ChangerMemberID, cancellationToken);
            var changerName = changer?.Resource?.FullName ?? "Someone";

            string oldDeadStr = domainEvent.OldDeadline?.ToString("yyyy-MM-dd") ?? "none";
            string newDeadStr = domainEvent.NewDeadline?.ToString("yyyy-MM-dd") ?? "none";

            foreach (var memberId in taskMembers)
            {
                var notification = new Notification
                {
                    RecipientID = memberId,
                    ActorID = domainEvent.ChangerMemberID,
                    NotificationType = "TaskDeadlineChanged",
                    Title = "Task Deadline Updated",
                    Message = $"{changerName} changed task '{domainEvent.TaskName}' deadline from '{oldDeadStr}' to '{newDeadStr}'.",
                    ReferenceType = "Task",
                    ReferenceID = domainEvent.TaskID,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync(cancellationToken);

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
