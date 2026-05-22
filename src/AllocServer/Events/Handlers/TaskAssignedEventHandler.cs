using AllocServer.Data;
using AllocServer.DTOs.Notifications;
using AllocServer.Events.DomainEvents;
using AllocServer.Interfaces.Notifications;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AllocServer.Events.Handlers
{
    public class TaskAssignedEventHandler : IEventHandler<TaskAssignedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationQueue _notificationQueue;

        public TaskAssignedEventHandler(ApplicationDbContext dbContext, INotificationQueue notificationQueue)
        {
            _dbContext = dbContext;
            _notificationQueue = notificationQueue;
        }

        public async Task HandleAsync(TaskAssignedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            if (domainEvent.AssigneeMemberID <= 0) return;
            if (domainEvent.AssigneeMemberID == domainEvent.AssignerMemberID) return;

            var assigner = await _dbContext.WorkspaceMembers
                .Include(m => m.Resource)
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == domainEvent.AssignerMemberID, cancellationToken);
                
            var assignerName = assigner?.Resource?.FullName ?? "Someone";

            var notification = new Notification
            {
                RecipientID = domainEvent.AssigneeMemberID,
                NotificationType = "TaskAssigned",
                Title = "New Task Assigned",
                Message = $"{assignerName} assigned you a task: {domainEvent.TaskName}",
                ReferenceType = "Task",
                ReferenceID = domainEvent.TaskID,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new { AssignerID = domainEvent.AssignerMemberID })
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
                CreatedAt = notification.CreatedAt,
                MetadataJson = notification.MetadataJson
            };

            var dispatchMessage = new NotificationDispatchMessage
            {
                RecipientID = notification.RecipientID,
                Payload = dto
            };

            await _notificationQueue.QueueNotificationAsync(dispatchMessage, cancellationToken);
        }
    }
}
