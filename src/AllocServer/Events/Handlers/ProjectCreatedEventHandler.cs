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
    public class ProjectCreatedEventHandler : IEventHandler<ProjectCreatedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationQueue _notificationQueue;

        public ProjectCreatedEventHandler(ApplicationDbContext dbContext, INotificationQueue notificationQueue)
        {
            _dbContext = dbContext;
            _notificationQueue = notificationQueue;
        }

        public async Task HandleAsync(ProjectCreatedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var members = await _dbContext.WorkspaceMembers
                .Where(m => m.WorkspaceID == domainEvent.WorkspaceID 
                         && m.WorkspaceMemberID != domainEvent.CreatorMemberID 
                         && m.Status == "Active")
                .ToListAsync(cancellationToken);

            foreach (var member in members)
            {
                var notification = new Notification
                {
                    RecipientID = member.WorkspaceMemberID,
                    ActorID = domainEvent.CreatorMemberID,
                    NotificationType = "ProjectCreated",
                    Title = "New Project Created",
                    Message = $"A new project '{domainEvent.ProjectName}' has been created in your workspace.",
                    ReferenceType = "Project",
                    ReferenceID = domainEvent.ProjectID,
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
