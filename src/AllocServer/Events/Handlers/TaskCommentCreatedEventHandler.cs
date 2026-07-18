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
    public class TaskCommentCreatedEventHandler : IEventHandler<TaskCommentCreatedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationQueue _notificationQueue;

        public TaskCommentCreatedEventHandler(ApplicationDbContext dbContext, INotificationQueue notificationQueue)
        {
            _dbContext = dbContext;
            _notificationQueue = notificationQueue;
        }

        public async Task HandleAsync(TaskCommentCreatedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var commenter = await _dbContext.WorkspaceMembers
                .Include(m => m.Resource)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == domainEvent.MemberID, cancellationToken);
            var commenterName = commenter?.Resource?.FullName ?? "Someone";

            // Lấy danh sách thành viên của task (Assignees, Watchers, Reviewers)
            var taskMembers = await _dbContext.TaskAssignees
                .Where(ta => ta.TaskID == domainEvent.TaskID 
                          && ta.WorkspaceMemberID != domainEvent.MemberID
                          && ta.WorkspaceMember.Status == "Active")
                .Select(ta => ta.WorkspaceMemberID)
                .Distinct()
                .ToListAsync(cancellationToken);

            var recipients = new HashSet<int>(taskMembers);

            // Nếu là comment reply, bổ sung người viết comment cha vào danh sách nhận
            if (domainEvent.ParentCommentID.HasValue)
            {
                var parentComment = await _dbContext.TaskComments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CommentID == domainEvent.ParentCommentID.Value, cancellationToken);
                
                if (parentComment != null && parentComment.MemberID != domainEvent.MemberID)
                {
                    var parentCommenter = await _dbContext.WorkspaceMembers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.WorkspaceMemberID == parentComment.MemberID && m.Status == "Active", cancellationToken);
                    
                    if (parentCommenter != null)
                    {
                        recipients.Add(parentCommenter.WorkspaceMemberID);
                    }
                }
            }

            foreach (var recipientId in recipients)
            {
                var notification = new Notification
                {
                    RecipientID = recipientId,
                    ActorID = domainEvent.MemberID,
                    NotificationType = "TaskCommentCreated",
                    Title = "New Comment on Task",
                    Message = $"{commenterName} commented on task '{domainEvent.TaskName}': \"{(domainEvent.Content.Length > 60 ? domainEvent.Content.Substring(0, 57) + "..." : domainEvent.Content)}\"",
                    ReferenceType = "Comment",
                    ReferenceID = domainEvent.CommentID,
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
