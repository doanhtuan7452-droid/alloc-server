using AllocServer.Data;
using AllocServer.DTOs.Tasks;
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

        private readonly ApplicationDbContext _context;

        public TaskService(ApplicationDbContext context)
        {
            _context = context;
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
                    CreatedAt = item.CreatedAt
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

            ValidateTaskDates(project, request.StartDate, request.EndDate);

            var task = new ProjectTask
            {
                ProjectID = project.ProjectID,
                TaskName = taskName,
                Status = status,
                DurationType = durationType,
                EstimatedValue = request.EstimatedValue,
                StartDate = request.StartDate,
                EndDate = request.EndDate
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

            ValidateTaskDates(project, request.StartDate, request.EndDate);

            task.TaskName = taskName;
            task.DurationType = durationType;
            task.EstimatedValue = request.EstimatedValue;
            task.StartDate = request.StartDate;
            task.EndDate = request.EndDate;
            task.Status = status;

            await _context.SaveChangesAsync();
            return MapTask(task);
        }

        public async Task<TaskAssigneeResponse> AssignTaskAssigneeAsync(
            ProjectTask task,
            AssignTaskAssigneeRequest request)
        {
            var assigneeType = NormalizeAssigneeType(request.AssigneeType);
            if (assigneeType == null)
            {
                throw new ArgumentException("AssigneeType chi nhan Assignee, Reviewer hoac Watcher.");
            }

            var taskWorkspaceId = await GetTaskWorkspaceIdAsync(task);

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
                CreatedAt = task.CreatedAt
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
