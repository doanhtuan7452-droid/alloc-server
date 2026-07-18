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
    public class TaskUnassignedEventHandler : IEventHandler<TaskUnassignedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationQueue _notificationQueue;

        public TaskUnassignedEventHandler(ApplicationDbContext dbContext, INotificationQueue notificationQueue)
        {
            _dbContext = dbContext;
            _notificationQueue = notificationQueue;
        }

        public async Task HandleAsync(TaskUnassignedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            if (domainEvent.MemberID <= 0) return;
            if (domainEvent.MemberID == domainEvent.AssignerMemberID) return;

            var recipient = await _dbContext.WorkspaceMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == domainEvent.MemberID && m.Status == "Active", cancellationToken);

            if (recipient == null) return;

            var assigner = await _dbContext.WorkspaceMembers
                .Include(m => m.Resource)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == domainEvent.AssignerMemberID, cancellationToken);
            
            var assignerName = assigner?.Resource?.FullName ?? "Someone";

            var notification = new Notification
            {
                RecipientID = domainEvent.MemberID,
                ActorID = domainEvent.AssignerMemberID,
                NotificationType = "TaskUnassigned",
                Title = "Removed From Task",
                Message = $"{assignerName} removed you from task: {domainEvent.TaskName}",
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
                RecipientID = domainEvent.MemberID,
                Payload = dto
            }, cancellationToken);
        }
    }
}
