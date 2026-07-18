using AllocServer.Data;
using AllocServer.DTOs.Notifications;
using AllocServer.Events.DomainEvents;
using AllocServer.Interfaces.Notifications;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Events.Handlers
{
    public class RiskNotificationEventHandler : IEventHandler<RiskNotificationEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationQueue _notificationQueue;

        public RiskNotificationEventHandler(ApplicationDbContext dbContext, INotificationQueue notificationQueue)
        {
            _dbContext = dbContext;
            _notificationQueue = notificationQueue;
        }

        public async Task HandleAsync(RiskNotificationEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var recipients = new HashSet<int>();

            if (domainEvent.RecipientMemberID.HasValue)
            {
                recipients.Add(domainEvent.RecipientMemberID.Value);
            }
            else
            {
                var project = await _dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectID == domainEvent.ProjectID, cancellationToken);
                if (project != null)
                {
                    var workspaceOwners = await _dbContext.WorkspaceMembers
                        .Where(m => m.WorkspaceID == project.WorkspaceID 
                                 && m.Status == "Active"
                                 && (m.WorkspaceRole.RoleName == "Owner" || m.WorkspaceRole.RoleName == "Manager"))
                        .Select(m => m.WorkspaceMemberID)
                        .ToListAsync(cancellationToken);
                    
                    foreach (var ownerId in workspaceOwners)
                    {
                        recipients.Add(ownerId);
                    }
                }
            }

            recipients.Remove(domainEvent.ActorMemberID);

            foreach (var recipientId in recipients)
            {
                var notification = new Notification
                {
                    RecipientID = recipientId,
                    ActorID = domainEvent.ActorMemberID,
                    NotificationType = "RiskNotification",
                    Title = "Risk Alert: " + domainEvent.RiskName,
                    Message = domainEvent.Message,
                    ReferenceType = "Risk",
                    ReferenceID = domainEvent.RiskID,
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
                    RecipientID = recipientId,
                    Payload = dto
                }, cancellationToken);
            }
        }
    }
}
