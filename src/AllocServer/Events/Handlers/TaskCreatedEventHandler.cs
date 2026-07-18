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
    public class TaskCreatedEventHandler : IEventHandler<TaskCreatedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationQueue _notificationQueue;

        public TaskCreatedEventHandler(ApplicationDbContext dbContext, INotificationQueue notificationQueue)
        {
            _dbContext = dbContext;
            _notificationQueue = notificationQueue;
        }

        public async Task HandleAsync(TaskCreatedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            // Lọc gửi cho Workspace Owner, Manager, và các thành viên đã có task trong cùng Project
            var project = await _dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectID == domainEvent.ProjectID, cancellationToken);
            var projectName = project?.ProjectName ?? "Project";

            var targetMembers = await _dbContext.WorkspaceMembers
                .Where(m => m.WorkspaceID == domainEvent.WorkspaceID 
                         && m.WorkspaceMemberID != domainEvent.CreatorMemberID
                         && m.Status == "Active")
                .Where(m => m.WorkspaceRole.RoleName == "Owner" 
                         || m.WorkspaceRole.RoleName == "Manager"
                         || _dbContext.TaskAssignees.Any(ta => ta.WorkspaceMemberID == m.WorkspaceMemberID && ta.Task.ProjectID == domainEvent.ProjectID))
                .ToListAsync(cancellationToken);

            foreach (var member in targetMembers)
            {
                var notification = new Notification
                {
                    RecipientID = member.WorkspaceMemberID,
                    ActorID = domainEvent.CreatorMemberID,
                    NotificationType = "TaskCreated",
                    Title = "New Task Created",
                    Message = $"Task '{domainEvent.TaskName}' was created in project '{projectName}'.",
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
                    RecipientID = member.WorkspaceMemberID,
                    Payload = dto
                }, cancellationToken);
            }
        }
    }
}
