using AllocServer.Data;
using AllocServer.DTOs.Notifications;
using AllocServer.Interfaces.Notifications;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace AllocServer.Services.Notification_Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedNotificationsResponse> GetNotificationsAsync(int accountId, GetNotificationsQuery query)
        {
            var activeMemberIds = await _context.WorkspaceMembers
                .Where(m => m.Resource.AccountID == accountId && m.Status == "Active" && !m.Resource.IsDeleted && !m.Workspace.IsDeleted)
                .Select(m => m.WorkspaceMemberID)
                .ToListAsync();

            var queryable = _context.Notifications
                .Where(n => activeMemberIds.Contains(n.RecipientID));

            if (query.IsRead.HasValue)
            {
                queryable = queryable.Where(n => n.IsRead == query.IsRead.Value);
            }

            if (!string.IsNullOrEmpty(query.ReferenceType))
            {
                queryable = queryable.Where(n => n.ReferenceType == query.ReferenceType);
            }

            // Note: WorkspaceId filter would require joining WorkspaceMembers or using Metadata
            // For now, if we assume WorkspaceId is stored in MetadataJson or mapped through Recipient
            // We can filter if needed. The plan said 'thêm workspaceId'.
            // To filter by WorkspaceId, we can join with WorkspaceMembers.
            if (query.WorkspaceId.HasValue)
            {
                var workspaceMemberIds = await _context.WorkspaceMembers
                    .Where(m => m.WorkspaceID == query.WorkspaceId.Value)
                    .Select(m => m.WorkspaceMemberID)
                    .ToListAsync();
                
                queryable = queryable.Where(n => workspaceMemberIds.Contains(n.RecipientID));
            }

            var totalCount = await queryable.CountAsync();
            var unreadCount = await _context.Notifications
                .Where(n => activeMemberIds.Contains(n.RecipientID) && !n.IsRead)
                .CountAsync();

            var notifications = await queryable
                .OrderByDescending(n => n.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(n => new NotificationDTO
                {
                    NotificationID = n.NotificationID,
                    NotificationType = n.NotificationType,
                    Title = n.Title,
                    Message = n.Message,
                    ReferenceType = n.ReferenceType,
                    ReferenceID = n.ReferenceID,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    CreatedAt = n.CreatedAt,
                    MetadataJson = n.MetadataJson
                })
                .ToListAsync();

            return new PagedNotificationsResponse
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize),
                UnreadCount = unreadCount,
                Items = notifications
            };
        }

        public async Task<int> GetUnreadCountAsync(int accountId)
        {
            var activeMemberIds = await _context.WorkspaceMembers
                .Where(m => m.Resource.AccountID == accountId && m.Status == "Active" && !m.Resource.IsDeleted && !m.Workspace.IsDeleted)
                .Select(m => m.WorkspaceMemberID)
                .ToListAsync();

            return await _context.Notifications
                .Where(n => activeMemberIds.Contains(n.RecipientID) && !n.IsRead)
                .CountAsync();
        }

        public async Task MarkAsReadAsync(int accountId, int notificationId)
        {
            var activeMemberIds = await _context.WorkspaceMembers
                .Where(m => m.Resource.AccountID == accountId && m.Status == "Active" && !m.Resource.IsDeleted && !m.Workspace.IsDeleted)
                .Select(m => m.WorkspaceMemberID)
                .ToListAsync();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationID == notificationId && activeMemberIds.Contains(n.RecipientID));

            if (notification == null)
            {
                throw new KeyNotFoundException("NotificationNotFound");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int accountId)
        {
            var activeMemberIds = await _context.WorkspaceMembers
                .Where(m => m.Resource.AccountID == accountId && m.Status == "Active" && !m.Resource.IsDeleted && !m.Workspace.IsDeleted)
                .Select(m => m.WorkspaceMemberID)
                .ToListAsync();

            var unreadNotifications = await _context.Notifications
                .Where(n => activeMemberIds.Contains(n.RecipientID) && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<NotificationDTO> GetNotificationDetailAsync(int accountId, int notificationId)
        {
            var activeMemberIds = await _context.WorkspaceMembers
                .Where(m => m.Resource.AccountID == accountId && m.Status == "Active" && !m.Resource.IsDeleted && !m.Workspace.IsDeleted)
                .Select(m => m.WorkspaceMemberID)
                .ToListAsync();

            var notification = await _context.Notifications
                .Where(n => n.NotificationID == notificationId && activeMemberIds.Contains(n.RecipientID))
                .Select(n => new NotificationDTO
                {
                    NotificationID = n.NotificationID,
                    NotificationType = n.NotificationType,
                    Title = n.Title,
                    Message = n.Message,
                    ReferenceType = n.ReferenceType,
                    ReferenceID = n.ReferenceID,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    CreatedAt = n.CreatedAt,
                    MetadataJson = n.MetadataJson
                })
                .FirstOrDefaultAsync();

            if (notification == null)
            {
                throw new KeyNotFoundException("NotificationNotFound");
            }

            if (notification.ReferenceType == "Task")
            {
                var taskRef = await _context.ProjectTasks
                    .Where(t =>
                        t.TaskID == notification.ReferenceID
                        && !t.IsDeleted
                        && t.Project != null
                        && !t.Project.IsDeleted)
                    .Select(t => new { t.TaskID, t.TaskName, t.Project!.WorkspaceID, t.ProjectID })
                    .FirstOrDefaultAsync();

                if (taskRef != null)
                {
                    notification.ReferenceData = new NotificationReferenceResponse
                    {
                        Type = "Task",
                        Id = taskRef.TaskID,
                        Title = taskRef.TaskName,
                        WorkspaceId = taskRef.WorkspaceID,
                        ProjectId = taskRef.ProjectID
                    };
                }
            }
            else if (notification.ReferenceType == "Comment")
            {
                var commentRef = await _context.TaskComments
                    .Where(c =>
                        c.CommentID == notification.ReferenceID
                        && !c.IsDeleted
                        && c.Task != null
                        && !c.Task.IsDeleted
                        && c.Task.Project != null
                        && !c.Task.Project.IsDeleted)
                    .Select(c => new { c.CommentID, c.Content, c.Task!.Project!.WorkspaceID, c.Task.ProjectID })
                    .FirstOrDefaultAsync();

                if (commentRef != null)
                {
                    notification.ReferenceData = new NotificationReferenceResponse
                    {
                        Type = "Comment",
                        Id = commentRef.CommentID,
                        Title = commentRef.Content.Length > 50 ? commentRef.Content.Substring(0, 50) + "..." : commentRef.Content,
                        WorkspaceId = commentRef.WorkspaceID,
                        ProjectId = commentRef.ProjectID
                    };
                }
            }
            else if (notification.ReferenceType == "Conversation")
            {
                var convRef = await _context.Conversations
                    .Where(c => c.ConversationID == notification.ReferenceID && !c.IsDeleted)
                    .Select(c => new { c.ConversationID, ConversationName = c.Name, c.WorkspaceID, c.ProjectID })
                    .FirstOrDefaultAsync();

                if (convRef != null)
                {
                    notification.ReferenceData = new NotificationReferenceResponse
                    {
                        Type = "Conversation",
                        Id = convRef.ConversationID,
                        Title = convRef.ConversationName ?? "Conversation",
                        WorkspaceId = convRef.WorkspaceID,
                        ProjectId = convRef.ProjectID
                    };
                }
            }
            else if (notification.ReferenceType == "Project")
            {
                if (notification.ReferenceID == 0)
                {
                    notification.ReferenceData = new NotificationReferenceResponse
                    {
                        Type = "LeaveRequest",
                        Id = 0,
                        Title = "Đơn nghỉ phép",
                        WorkspaceId = 0,
                        ProjectId = 0
                    };
                }
                else
                {
                    var projRef = await _context.Projects
                        .Where(p => p.ProjectID == notification.ReferenceID && !p.IsDeleted)
                        .Select(p => new { p.ProjectID, p.ProjectName, p.WorkspaceID })
                        .FirstOrDefaultAsync();

                    if (projRef != null)
                    {
                        notification.ReferenceData = new NotificationReferenceResponse
                        {
                            Type = "Project",
                            Id = projRef.ProjectID,
                            Title = projRef.ProjectName,
                            WorkspaceId = projRef.WorkspaceID,
                            ProjectId = projRef.ProjectID
                        };
                    }
                }
            }
            else if (notification.ReferenceType == "Risk")
            {
                var riskRef = await _context.Risks
                    .Where(r =>
                        r.RiskID == notification.ReferenceID
                        && !r.IsDeleted
                        && r.Project != null
                        && !r.Project.IsDeleted)
                    .Select(r => new { r.RiskID, r.RiskName, r.Project!.WorkspaceID, r.ProjectID })
                    .FirstOrDefaultAsync();

                if (riskRef != null)
                {
                    notification.ReferenceData = new NotificationReferenceResponse
                    {
                        Type = "Risk",
                        Id = riskRef.RiskID,
                        Title = riskRef.RiskName,
                        WorkspaceId = riskRef.WorkspaceID,
                        ProjectId = riskRef.ProjectID
                    };
                }
            }

            return notification;
        }

        public async Task RegisterDeviceTokenAsync(int accountId, RegisterDeviceTokenRequest request)
        {
            var deviceToken = request.DeviceToken.Trim();
            var deviceType = request.DeviceType.Trim();

            var existingToken = await _context.NotificationDeviceTokens
                .FirstOrDefaultAsync(t => t.DeviceToken == deviceToken);

            if (existingToken != null)
            {
                RefreshDeviceToken(existingToken, accountId, deviceType);
            }
            else
            {
                var newToken = new NotificationDeviceToken
                {
                    AccountID = accountId,
                    DeviceType = deviceType,
                    DeviceToken = deviceToken,
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    IsActive = true,
                    FailureCount = 0,
                    LastError = null
                };
                _context.NotificationDeviceTokens.Add(newToken);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                DetachAddedDeviceTokens();

                var token = await _context.NotificationDeviceTokens
                    .FirstAsync(item => item.DeviceToken == deviceToken);

                RefreshDeviceToken(token, accountId, deviceType);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeDeviceTokenAsync(int accountId, RevokeDeviceTokenRequest request)
        {
            var deviceToken = request.DeviceToken.Trim();

            var token = await _context.NotificationDeviceTokens
                .FirstOrDefaultAsync(t => t.DeviceToken == deviceToken && t.AccountID == accountId);

            if (token != null)
            {
                token.IsActive = false;
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private static void RefreshDeviceToken(
            NotificationDeviceToken token,
            int accountId,
            string deviceType)
        {
            token.AccountID = accountId;
            token.DeviceType = deviceType;
            token.LastUsedAt = DateTime.UtcNow;
            token.IsActive = true;
            token.RevokedAt = null;
            token.FailureCount = 0;
            token.LastError = null;
        }

        private void DetachAddedDeviceTokens()
        {
            var addedDeviceTokens = _context.ChangeTracker
                .Entries<NotificationDeviceToken>()
                .Where(entry => entry.State == EntityState.Added)
                .ToList();

            foreach (var entry in addedDeviceTokens)
            {
                entry.State = EntityState.Detached;
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException
                   && (sqlException.Number == 2601 || sqlException.Number == 2627);
        }
    }
}
