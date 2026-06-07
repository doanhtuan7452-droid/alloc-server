using AllocServer.Data;
using AllocServer.DTOs.Tasks;
using AllocServer.Events;
using AllocServer.Events.DomainEvents;
using AllocServer.Constants.Permissions;
using AllocServer.Filters;
using AllocServer.Interfaces.Tasks;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Task_Services
{
    public class TaskService : ITaskService
    {
        private const decimal MinEstimatedValue = 0.01m;
        private const decimal MaxEstimatedValue = 99999999.99m;

        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
        {
            "To-do",
            "In Progress",
            "Review",
            "Done"
        };

        private static readonly HashSet<string> AllowedDurationTypes = new(StringComparer.Ordinal)
        {
            "Hour",
            "Day",
            "StoryPoint"
        };

        private static readonly HashSet<string> AllowedAssigneeTypes = new(StringComparer.Ordinal)
        {
            "Assignee",
            "Reviewer",
            "Watcher"
        };

        private static readonly HashSet<string> AllowedDependencyTypes = new(StringComparer.Ordinal)
        {
            "FS",
            "SS",
            "FF",
            "SF"
        };

        private static readonly HashSet<string> AllowedComplexities = new(StringComparer.Ordinal)
        {
            "Low",
            "Medium",
            "High",
            "Critical"
        };

        private static readonly HashSet<string> AllowedSkillLevels = new(StringComparer.Ordinal)
        {
            "Low",
            "Medium",
            "High",
            "Expert"
        };

        private static readonly HashSet<string> AllowedPriorities = new(StringComparer.Ordinal)
        {
            "Low",
            "Medium",
            "High",
            "Critical"
        };

        private readonly ApplicationDbContext _context;
        private readonly IEventPublisher _eventPublisher;

        public TaskService(ApplicationDbContext context, IEventPublisher eventPublisher)
        {
            _context = context;
            _eventPublisher = eventPublisher;
        }

        public async Task<PagedProjectTasksResponse> GetProjectTasksAsync(
            Project project,
            GetProjectTasksQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var search = NormalizeOptionalString(query.Search);
            var status = NormalizeTaskStatus(query.Status, allowDefault: false);
            var durationType = NormalizeDurationType(query.DurationType);

            if (!string.IsNullOrWhiteSpace(query.Status) && status == null)
            {
                throw new ArgumentException("Status chi nhan To-do, In Progress, Review hoac Done.");
            }

            if (!string.IsNullOrWhiteSpace(query.DurationType) && durationType == null)
            {
                throw new ArgumentException("DurationType chi nhan Hour, Day hoac StoryPoint.");
            }

            var complexity = NormalizeOptionalComplexity(query.Complexity);
            var skillLevel = NormalizeOptionalRequiredSkillLevel(query.RequiredSkillLevel);
            var priority = NormalizeOptionalPriority(query.Priority);

            if (query.StartDateFrom != null
                && query.StartDateTo != null
                && query.StartDateTo < query.StartDateFrom)
            {
                throw new ArgumentException("startDateTo phai lon hon hoac bang startDateFrom.");
            }

            if (query.EndDateFrom != null
                && query.EndDateTo != null
                && query.EndDateTo < query.EndDateFrom)
            {
                throw new ArgumentException("endDateTo phai lon hon hoac bang endDateFrom.");
            }

            var tasksQuery = _context.ProjectTasks
                .AsNoTracking()
                .Where(item => item.ProjectID == project.ProjectID);

            if (!string.IsNullOrEmpty(search))
            {
                tasksQuery = tasksQuery.Where(item => item.TaskName.Contains(search));
            }

            if (status != null)
            {
                tasksQuery = tasksQuery.Where(item => item.Status == status);
            }

            if (durationType != null)
            {
                tasksQuery = tasksQuery.Where(item => item.DurationType == durationType);
            }

            if (complexity != null)
            {
                tasksQuery = tasksQuery.Where(item => item.Complexity == complexity);
            }

            if (skillLevel != null)
            {
                tasksQuery = tasksQuery.Where(item => item.RequiredSkillLevel == skillLevel);
            }

            if (priority != null)
            {
                tasksQuery = tasksQuery.Where(item => item.Priority == priority);
            }

            if (query.StartDateFrom != null)
            {
                tasksQuery = tasksQuery.Where(item =>
                    item.StartDate != null
                    && item.StartDate >= query.StartDateFrom);
            }

            if (query.StartDateTo != null)
            {
                tasksQuery = tasksQuery.Where(item =>
                    item.StartDate != null
                    && item.StartDate <= query.StartDateTo);
            }

            if (query.EndDateFrom != null)
            {
                tasksQuery = tasksQuery.Where(item =>
                    item.EndDate != null
                    && item.EndDate >= query.EndDateFrom);
            }

            if (query.EndDateTo != null)
            {
                tasksQuery = tasksQuery.Where(item =>
                    item.EndDate != null
                    && item.EndDate <= query.EndDateTo);
            }

            var totalItems = await tasksQuery.CountAsync();
            var items = await tasksQuery
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.TaskID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new ProjectTaskListItemResponse
                {
                    TaskID = item.TaskID,
                    ProjectID = item.ProjectID,
                    TaskName = item.TaskName,
                    Status = item.Status,
                    DurationType = item.DurationType,
                    EstimatedValue = item.EstimatedValue,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    CreatedAt = item.CreatedAt,
                    Complexity = item.Complexity,
                    RequiredSkillLevel = item.RequiredSkillLevel,
                    Priority = item.Priority,
                    ExpectedTeamSize = item.ExpectedTeamSize
                })
                .ToListAsync();

            return new PagedProjectTasksResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<ProjectTaskDetailResponse> CreateProjectTaskAsync(
            int accountId,
            Project project,
            CreateProjectTaskRequest request)
        {
            var taskName = NormalizeOptionalString(request.TaskName);
            if (taskName == null)
            {
                throw new ArgumentException("Ten task khong duoc de trong.");
            }

            var durationType = NormalizeDurationType(request.DurationType);
            if (durationType == null)
            {
                throw new ArgumentException("DurationType chi nhan Hour, Day hoac StoryPoint.");
            }

            var status = NormalizeTaskStatus(request.Status, allowDefault: true);
            if (status == null)
            {
                throw new ArgumentException("Status chi nhan To-do, In Progress, Review hoac Done.");
            }

            if (request.EstimatedValue < MinEstimatedValue
                || request.EstimatedValue > MaxEstimatedValue)
            {
                throw new ArgumentException("Gia tri uoc tinh phai tu 0.01 den 99999999.99.");
            }

            var complexity = NormalizeOptionalComplexity(request.Complexity) ?? "Medium";
            var requiredSkillLevel = NormalizeOptionalRequiredSkillLevel(request.RequiredSkillLevel) ?? "Medium";
            var priority = NormalizeOptionalPriority(request.Priority) ?? "Medium";
            var expectedTeamSize = request.ExpectedTeamSize;
            if (expectedTeamSize < 1)
            {
                throw new ArgumentException("So luong thanh vien du kien (ExpectedTeamSize) phai lon hon hoac bang 1.");
            }

            ValidateTaskDates(project, request.StartDate, request.EndDate);

            var task = new ProjectTask
            {
                ProjectID = project.ProjectID,
                TaskName = taskName,
                Status = status,
                DurationType = durationType,
                EstimatedValue = request.EstimatedValue,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Complexity = complexity,
                RequiredSkillLevel = requiredSkillLevel,
                Priority = priority,
                ExpectedTeamSize = expectedTeamSize
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();

            return MapTask(task);
        }

        public async Task<ProjectTaskDetailResponse> UpdateProjectTaskAsync(
            ProjectTask task,
            Project project,
            UpdateProjectTaskRequest request)
        {
            var taskName = NormalizeOptionalString(request.TaskName);
            if (taskName == null)
            {
                throw new ArgumentException("Ten task khong duoc de trong.");
            }

            var durationType = NormalizeDurationType(request.DurationType);
            if (durationType == null)
            {
                throw new ArgumentException("DurationType chi nhan Hour, Day hoac StoryPoint.");
            }

            var status = NormalizeTaskStatus(request.Status, allowDefault: false);
            if (status == null)
            {
                throw new ArgumentException("Status chi nhan To-do, In Progress, Review hoac Done.");
            }

            if (request.EstimatedValue < MinEstimatedValue
                || request.EstimatedValue > MaxEstimatedValue)
            {
                throw new ArgumentException("Gia tri uoc tinh phai tu 0.01 den 99999999.99.");
            }

            var complexity = NormalizeOptionalComplexity(request.Complexity);
            if (complexity == null)
            {
                throw new ArgumentException("Do phuc tap (Complexity) la bat buoc.");
            }

            var requiredSkillLevel = NormalizeOptionalRequiredSkillLevel(request.RequiredSkillLevel);
            if (requiredSkillLevel == null)
            {
                throw new ArgumentException("Yeu cau trinh do (RequiredSkillLevel) la bat buoc.");
            }

            var priority = NormalizeOptionalPriority(request.Priority);
            if (priority == null)
            {
                throw new ArgumentException("Muc do uu tien (Priority) la bat buoc.");
            }

            if (request.ExpectedTeamSize == null)
            {
                throw new ArgumentException("So luong thanh vien du kien (ExpectedTeamSize) la bat buoc.");
            }
            var expectedTeamSize = request.ExpectedTeamSize.Value;
            if (expectedTeamSize < 1)
            {
                throw new ArgumentException("So luong thanh vien du kien (ExpectedTeamSize) phai lon hon hoac bang 1.");
            }

            ValidateTaskDates(project, request.StartDate, request.EndDate);

            var oldStatus = task.Status;

            task.TaskName = taskName;
            task.DurationType = durationType;
            task.EstimatedValue = request.EstimatedValue;
            task.StartDate = request.StartDate;
            task.EndDate = request.EndDate;
            task.Status = status;
            task.Complexity = complexity;
            task.RequiredSkillLevel = requiredSkillLevel;
            task.Priority = priority;
            task.ExpectedTeamSize = expectedTeamSize;

            await _context.SaveChangesAsync();

            if (oldStatus != status)
            {
                await _eventPublisher.PublishAsync(new TaskStatusChangedEvent(task.TaskID, oldStatus, status));
            }

            return MapTask(task);
        }

        public async Task<TaskAssigneeResponse> AssignTaskAssigneeAsync(
            int accountId,
            ProjectTask task,
            AssignTaskAssigneeRequest request)
        {
            var assigneeType = NormalizeAssigneeType(request.AssigneeType);
            if (assigneeType == null)
            {
                throw new ArgumentException("AssigneeType chi nhan Assignee, Reviewer hoac Watcher.");
            }

            var taskWorkspaceId = await GetTaskWorkspaceIdAsync(task);
            var assignerMemberId = await GetWorkspaceMemberIdAsync(accountId, taskWorkspaceId);

            var isValidMember = await _context.WorkspaceMembers
                .AsNoTracking()
                .AnyAsync(member =>
                    member.WorkspaceMemberID == request.MemberId
                    && member.WorkspaceID == taskWorkspaceId
                    && member.Status == "Active"
                    && !member.Workspace.IsDeleted
                    && !member.Resource.IsDeleted
                    && _context.Accounts.Any(account =>
                        account.AccountID == member.Resource.AccountID));

            if (!isValidMember)
            {
                throw new ArgumentException("Thanh vien khong ton tai, khong active hoac khong thuoc workspace cua task.");
            }

            var exists = await _context.TaskAssignees
                .AsNoTracking()
                .AnyAsync(item =>
                    item.TaskID == task.TaskID
                    && item.WorkspaceMemberID == request.MemberId
                    && item.AssigneeType == assigneeType);

            if (exists)
            {
                throw new InvalidOperationException("Thanh vien da duoc gan vai tro nay trong task.");
            }

            var taskAssignee = new TaskAssignee
            {
                TaskID = task.TaskID,
                WorkspaceMemberID = request.MemberId,
                AssigneeType = assigneeType,
                AssignedAt = DateTime.UtcNow
            };

            _context.TaskAssignees.Add(taskAssignee);
            await _context.SaveChangesAsync();

            await _eventPublisher.PublishAsync(new TaskAssignedEvent(task.TaskID, request.MemberId, assignerMemberId, task.TaskName));

            return MapTaskAssignee(taskAssignee);
        }

        public async Task<bool> RemoveTaskAssigneeAsync(
            ProjectTask task,
            int workspaceMemberId)
        {
            if (workspaceMemberId <= 0)
            {
                throw new ArgumentException("memberId phai lon hon 0.");
            }

            var deletedCount = await _context.TaskAssignees
                .Where(item =>
                    item.TaskID == task.TaskID
                    && item.WorkspaceMemberID == workspaceMemberId)
                .ExecuteDeleteAsync();

            return deletedCount > 0;
        }

        public async Task<TaskDependencyResponse> CreateTaskDependencyAsync(
            ProjectTask successorTask,
            CreateTaskDependencyRequest request)
        {
            var dependencyType = NormalizeDependencyType(request.DependencyType);
            if (dependencyType == null)
            {
                throw new ArgumentException("DependencyType chi nhan FS, SS, FF hoac SF.");
            }

            if (request.PredecessorTaskId == successorTask.TaskID)
            {
                throw new ArgumentException("Task khong the phu thuoc vao chinh no.");
            }

            var predecessorTask = await _context.ProjectTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.TaskID == request.PredecessorTaskId);

            if (predecessorTask == null)
            {
                throw new KeyNotFoundException("Khong tim thay predecessor task.");
            }

            if (predecessorTask.ProjectID != successorTask.ProjectID)
            {
                throw new ArgumentException("Predecessor task phai thuoc cung project voi successor task.");
            }

            var exists = await _context.TaskDependencies
                .AsNoTracking()
                .AnyAsync(item =>
                    item.PredecessorTaskID == request.PredecessorTaskId
                    && item.SuccessorTaskID == successorTask.TaskID
                    && item.DependencyType == dependencyType);

            if (exists)
            {
                throw new InvalidOperationException("Dependency nay da ton tai.");
            }

            var createsCycle = await HasDependencyPathAsync(
                startTaskId: successorTask.TaskID,
                targetTaskId: request.PredecessorTaskId);

            if (createsCycle)
            {
                throw new InvalidOperationException("Khong the tao dependency vi se tao vong lap.");
            }

            var dependency = new TaskDependency
            {
                PredecessorTaskID = request.PredecessorTaskId,
                SuccessorTaskID = successorTask.TaskID,
                DependencyType = dependencyType
            };

            _context.TaskDependencies.Add(dependency);
            await _context.SaveChangesAsync();

            return MapTaskDependency(dependency);
        }

        public async Task DeleteProjectTaskAsync(int accountId, ProjectTask task)
        {
            var hasTimesheets = await _context.Timesheets
                .AsNoTracking()
                .AnyAsync(item => item.TaskID == task.TaskID);

            if (hasTimesheets)
            {
                throw new InvalidOperationException(
                    "Khong the xoa Task da co du lieu ghi nhan thoi gian. Vui long chuyen trang thai Task thay vi xoa.");
            }

            var deletedAt = DateTime.UtcNow;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.TaskAssignees
                    .Where(item => item.TaskID == task.TaskID)
                    .ExecuteDeleteAsync();

                await _context.TaskDependencies
                    .Where(item =>
                        item.PredecessorTaskID == task.TaskID
                        || item.SuccessorTaskID == task.TaskID)
                    .ExecuteDeleteAsync();

                await _context.TaskComments
                    .Where(item => item.TaskID == task.TaskID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.OTRequests
                    .Where(item => item.TaskID == task.TaskID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.TaskAssets
                    .Where(item => item.TaskID == task.TaskID)
                    .ExecuteDeleteAsync();

                await _context.Risks
                    .Where(item => item.TaskID == task.TaskID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                task.IsDeleted = true;
                task.DeletedAt = deletedAt;
                task.DeletedBy = accountId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<TaskCommentResponse>> GetTaskCommentsAsync(ProjectTask task)
        {
            var comments = await _context.TaskComments
                .Include(c => c.WorkspaceMember)
                .ThenInclude(m => m.Resource)
                .AsNoTracking()
                .Where(c => c.TaskID == task.TaskID)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var commentDict = comments.Select(c => new TaskCommentResponse
            {
                CommentId = c.CommentID,
                TaskId = c.TaskID,
                MemberId = c.MemberID,
                MemberName = c.WorkspaceMember?.Resource?.FullName ?? string.Empty,
                MemberAvatarUrl = c.WorkspaceMember?.Resource?.AvatarURL,
                ParentCommentId = c.ParentCommentID,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToDictionary(c => c.CommentId);

            var rootComments = new List<TaskCommentResponse>();

            foreach (var comment in commentDict.Values)
            {
                if (comment.ParentCommentId.HasValue && commentDict.TryGetValue(comment.ParentCommentId.Value, out var parent))
                {
                    parent.Replies.Add(comment);
                }
                else
                {
                    rootComments.Add(comment);
                }
            }

            return rootComments;
        }

        public async Task<TaskCommentResponse> CreateTaskCommentAsync(
            int accountId,
            ProjectTask task,
            CreateTaskCommentRequest request)
        {
            var content = NormalizeOptionalString(request.Content);
            if (content == null)
                throw new ArgumentException("Noi dung binh luan khong duoc de trong.");

            var taskWorkspaceId = await GetTaskWorkspaceIdAsync(task);
            var memberId = await GetWorkspaceMemberIdAsync(accountId, taskWorkspaceId);

            int? finalParentId = null;
            if (request.ParentCommentId.HasValue)
            {
                var parent = await _context.TaskComments
                    .FirstOrDefaultAsync(c => c.CommentID == request.ParentCommentId.Value && c.TaskID == task.TaskID);

                if (parent == null)
                    throw new KeyNotFoundException("Khong tim thay binh luan cha.");

                finalParentId = parent.ParentCommentID ?? parent.CommentID; // Force 1-level nesting
            }

            var comment = new TaskComment
            {
                TaskID = task.TaskID,
                MemberID = memberId,
                ParentCommentID = finalParentId,
                Content = content
            };

            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            var memberInfo = await _context.WorkspaceMembers
                .Include(m => m.Resource)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == memberId);

            return new TaskCommentResponse
            {
                CommentId = comment.CommentID,
                TaskId = comment.TaskID,
                MemberId = comment.MemberID,
                MemberName = memberInfo?.Resource?.FullName ?? string.Empty,
                MemberAvatarUrl = memberInfo?.Resource?.AvatarURL,
                ParentCommentId = comment.ParentCommentID,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<TaskCommentResponse> UpdateTaskCommentAsync(
            int accountId,
            int commentId,
            UpdateTaskCommentRequest request)
        {
            var content = NormalizeOptionalString(request.Content);
            if (content == null)
                throw new ArgumentException("Noi dung binh luan khong duoc de trong.");

            var comment = await _context.TaskComments
                .Include(c => c.Task)
                    .ThenInclude(t => t.Project)
                .Include(c => c.WorkspaceMember)
                    .ThenInclude(m => m.Resource)
                .FirstOrDefaultAsync(c => c.CommentID == commentId);

            if (comment == null)
                throw new KeyNotFoundException("Khong tim thay binh luan.");

            var workspaceId = comment.Task!.Project!.WorkspaceID;
            var currentMemberId = await GetWorkspaceMemberIdAsync(accountId, workspaceId);

            var currentMember = await _context.WorkspaceMembers
                .Include(m => m.WorkspaceRole)
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == currentMemberId);

            var isOwner = currentMember?.WorkspaceRole?.RoleName == "Owner";
            var hasModeratePermission = await _context.RolePermissions
                .AnyAsync(rp => rp.WorkspaceRoleID == currentMember!.WorkspaceRoleID && rp.PermissionID == TaskPermissionIds.Update);

            if (comment.MemberID != currentMemberId && !isOwner && !hasModeratePermission)
                throw new UnauthorizedAccessException("Ban khong co quyen sua binh luan nay.");

            comment.Content = content;
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new TaskCommentResponse
            {
                CommentId = comment.CommentID,
                TaskId = comment.TaskID,
                MemberId = comment.MemberID,
                MemberName = comment.WorkspaceMember?.Resource?.FullName ?? string.Empty,
                MemberAvatarUrl = comment.WorkspaceMember?.Resource?.AvatarURL,
                ParentCommentId = comment.ParentCommentID,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }

        public async Task DeleteTaskCommentAsync(int accountId, int commentId)
        {
            var comment = await _context.TaskComments
                .Include(c => c.Task)
                    .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(c => c.CommentID == commentId);

            if (comment == null)
                throw new KeyNotFoundException("Khong tim thay binh luan.");

            var workspaceId = comment.Task!.Project!.WorkspaceID;
            var currentMemberId = await GetWorkspaceMemberIdAsync(accountId, workspaceId);

            var currentMember = await _context.WorkspaceMembers
                .Include(m => m.WorkspaceRole)
                .FirstOrDefaultAsync(m => m.WorkspaceMemberID == currentMemberId);

            var isOwner = currentMember?.WorkspaceRole?.RoleName == "Owner";
            var hasModeratePermission = await _context.RolePermissions
                .AnyAsync(rp => rp.WorkspaceRoleID == currentMember!.WorkspaceRoleID && rp.PermissionID == TaskPermissionIds.Update);

            if (comment.MemberID != currentMemberId && !isOwner && !hasModeratePermission)
                throw new UnauthorizedAccessException("Ban khong co quyen xoa binh luan nay.");

            var deletedAt = DateTime.UtcNow;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.TaskComments
                    .Where(c => c.CommentID == commentId || c.ParentCommentID == commentId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(c => c.IsDeleted, true)
                        .SetProperty(c => c.DeletedAt, deletedAt)
                        .SetProperty(c => c.DeletedBy, accountId));

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<TaskAssetResponse>> GetTaskAssetsAsync(ProjectTask task)
        {
            return await _context.TaskAssets
                .Include(ta => ta.Asset)
                .Include(ta => ta.AttachedByMember)
                    .ThenInclude(m => m.Resource)
                .AsNoTracking()
                .Where(ta => ta.TaskID == task.TaskID && ta.Asset != null && !ta.Asset.IsDeleted)
                .OrderByDescending(ta => ta.AttachedAt)
                .Select(ta => new TaskAssetResponse
                {
                    AssetId = ta.AssetID,
                    AssetName = ta.Asset!.AssetName,
                    AssetType = ta.Asset.AssetType,
                    FileSizeKB = ta.Asset.FileSizeKB,
                    AttachedBy = ta.AttachedBy,
                    AttachedByName = ta.AttachedByMember!.Resource!.FullName,
                    AttachedAt = ta.AttachedAt
                })
                .ToListAsync();
        }

        public async Task<List<TaskAssetResponse>> AttachTaskAssetsAsync(
            int accountId,
            ProjectTask task,
            AttachTaskAssetRequest request)
        {
            if (request.AssetIds == null || !request.AssetIds.Any())
                throw new ArgumentException("Danh sach AssetIds khong duoc rong.");

            var uniqueAssetIds = request.AssetIds.Distinct().ToList();

            var validAssets = await _context.ProjectAssets
                .AsNoTracking()
                .Where(a => uniqueAssetIds.Contains(a.AssetID) && a.ProjectID == task.ProjectID)
                .ToListAsync();

            if (validAssets.Count != uniqueAssetIds.Count)
                throw new ArgumentException("Mot hoac nhieu Asset khong hop le (khong ton tai, da bi xoa hoac thuoc project khac).");

            var existingLinks = await _context.TaskAssets
                .AsNoTracking()
                .Where(ta => ta.TaskID == task.TaskID && uniqueAssetIds.Contains(ta.AssetID))
                .Select(ta => ta.AssetID)
                .ToListAsync();

            var newAssetIds = uniqueAssetIds.Except(existingLinks).ToList();
            if (!newAssetIds.Any())
                return await GetTaskAssetsAsync(task);

            var taskWorkspaceId = await GetTaskWorkspaceIdAsync(task);
            var memberId = await GetWorkspaceMemberIdAsync(accountId, taskWorkspaceId);

            var newAttachments = newAssetIds.Select(id => new TaskAsset
            {
                TaskID = task.TaskID,
                AssetID = id,
                AttachedBy = memberId,
                AttachedAt = DateTime.UtcNow
            }).ToList();

            _context.TaskAssets.AddRange(newAttachments);
            await _context.SaveChangesAsync();

            return await GetTaskAssetsAsync(task);
        }

        public async Task DetachTaskAssetAsync(
            int accountId,
            ProjectTask task,
            int assetId)
        {
            var deletedCount = await _context.TaskAssets
                .Where(ta => ta.TaskID == task.TaskID && ta.AssetID == assetId)
                .ExecuteDeleteAsync();

            if (deletedCount == 0)
                throw new KeyNotFoundException("Khong tim thay lien ket tai lieu voi task.");
        }

        private async Task<int> GetWorkspaceMemberIdAsync(int accountId, int workspaceId)
        {
            var member = await _context.WorkspaceMembers
                .Include(m => m.Resource)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WorkspaceID == workspaceId && m.Resource.AccountID == accountId && !m.Resource.IsDeleted && m.Status == "Active");

            if (member == null)
                throw new UnauthorizedAccessException("Thanh vien khong thuoc workspace nay.");

            return member.WorkspaceMemberID;
        }

        private static void ValidateTaskDates(
            Project project,
            DateOnly? startDate,
            DateOnly? endDate)
        {
            if (startDate == null && endDate == null)
                return;

            if (startDate != null && endDate != null && endDate < startDate)
            {
                throw new ArgumentException("Ngay ket thuc task phai lon hon hoac bang ngay bat dau task.");
            }

            if (startDate != null && startDate < project.StartDate)
            {
                throw new ArgumentException("Ngay bat dau task khong duoc nho hon ngay bat dau project.");
            }

            if (endDate != null && endDate > project.EndDate)
            {
                throw new ArgumentException("Ngay ket thuc task khong duoc lon hon ngay ket thuc project.");
            }
        }

        private static ProjectTaskDetailResponse MapTask(ProjectTask task)
        {
            return new ProjectTaskDetailResponse
            {
                TaskID = task.TaskID,
                ProjectID = task.ProjectID,
                TaskName = task.TaskName,
                Status = task.Status,
                DurationType = task.DurationType,
                EstimatedValue = task.EstimatedValue,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                CreatedAt = task.CreatedAt,
                Complexity = task.Complexity,
                RequiredSkillLevel = task.RequiredSkillLevel,
                Priority = task.Priority,
                ExpectedTeamSize = task.ExpectedTeamSize
            };
        }

        private async Task<int> GetTaskWorkspaceIdAsync(ProjectTask task)
        {
            if (task.Project != null)
            {
                return task.Project.WorkspaceID;
            }

            return await _context.Projects
                .Where(project => project.ProjectID == task.ProjectID)
                .Select(project => project.WorkspaceID)
                .FirstAsync();
        }

        private async Task<bool> HasDependencyPathAsync(
            int startTaskId,
            int targetTaskId)
        {
            var visitedTaskIds = new HashSet<int>();
            var pendingTaskIds = new Queue<int>();
            pendingTaskIds.Enqueue(startTaskId);

            while (pendingTaskIds.Count > 0)
            {
                var currentTaskId = pendingTaskIds.Dequeue();
                if (!visitedTaskIds.Add(currentTaskId))
                {
                    continue;
                }

                var successorTaskIds = await _context.TaskDependencies
                    .AsNoTracking()
                    .Where(item => item.PredecessorTaskID == currentTaskId)
                    .Select(item => item.SuccessorTaskID)
                    .ToListAsync();

                foreach (var successorTaskId in successorTaskIds)
                {
                    if (successorTaskId == targetTaskId)
                    {
                        return true;
                    }

                    pendingTaskIds.Enqueue(successorTaskId);
                }
            }

            return false;
        }

        private static TaskAssigneeResponse MapTaskAssignee(TaskAssignee taskAssignee)
        {
            return new TaskAssigneeResponse
            {
                TaskId = taskAssignee.TaskID,
                MemberId = taskAssignee.WorkspaceMemberID,
                AssigneeType = taskAssignee.AssigneeType,
                AssignedAt = taskAssignee.AssignedAt
            };
        }

        private static TaskDependencyResponse MapTaskDependency(TaskDependency taskDependency)
        {
            return new TaskDependencyResponse
            {
                DependencyId = taskDependency.DependencyID,
                PredecessorTaskId = taskDependency.PredecessorTaskID,
                SuccessorTaskId = taskDependency.SuccessorTaskID,
                DependencyType = taskDependency.DependencyType
            };
        }

        private static string? NormalizeOptionalComplexity(string? complexity)
        {
            var normalized = NormalizeOptionalString(complexity);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "LOW" => "Low",
                "MEDIUM" => "Medium",
                "HIGH" => "High",
                "CRITICAL" => "Critical",
                _ => normalized
            };

            if (!AllowedComplexities.Contains(candidate))
            {
                throw new ArgumentException("Do phuc tap (Complexity) phai la Low, Medium, High hoac Critical.");
            }

            return candidate;
        }

        private static string? NormalizeOptionalRequiredSkillLevel(string? skillLevel)
        {
            var normalized = NormalizeOptionalString(skillLevel);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "LOW" => "Low",
                "MEDIUM" => "Medium",
                "HIGH" => "High",
                "EXPERT" => "Expert",
                _ => normalized
            };

            if (!AllowedSkillLevels.Contains(candidate))
            {
                throw new ArgumentException("Yeu cau trinh do (RequiredSkillLevel) phai la Low, Medium, High hoac Expert.");
            }

            return candidate;
        }

        private static string? NormalizeOptionalPriority(string? priority)
        {
            var normalized = NormalizeOptionalString(priority);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "LOW" => "Low",
                "MEDIUM" => "Medium",
                "HIGH" => "High",
                "CRITICAL" => "Critical",
                _ => normalized
            };

            if (!AllowedPriorities.Contains(candidate))
            {
                throw new ArgumentException("Muc do uu tien (Priority) phai la Low, Medium, High hoac Critical.");
            }

            return candidate;
        }

        private static string? NormalizeTaskStatus(string? status, bool allowDefault)
        {
            var normalized = NormalizeOptionalString(status);
            if (normalized == null)
                return allowDefault ? "To-do" : null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "TODO" or "TO DO" or "TO-DO" => "To-do",
                "INPROGRESS" or "IN PROGRESS" => "In Progress",
                "REVIEW" => "Review",
                "DONE" => "Done",
                _ => normalized
            };

            return AllowedStatuses.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeDurationType(string? durationType)
        {
            var normalized = NormalizeOptionalString(durationType);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "HOUR" => "Hour",
                "DAY" => "Day",
                "STORYPOINT" or "STORY POINT" => "StoryPoint",
                _ => normalized
            };

            return AllowedDurationTypes.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeAssigneeType(string? assigneeType)
        {
            var normalized = NormalizeOptionalString(assigneeType);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "ASSIGNEE" => "Assignee",
                "REVIEWER" => "Reviewer",
                "WATCHER" => "Watcher",
                _ => normalized
            };

            return AllowedAssigneeTypes.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeDependencyType(string? dependencyType)
        {
            var normalized = NormalizeOptionalString(dependencyType);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant();
            return AllowedDependencyTypes.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
