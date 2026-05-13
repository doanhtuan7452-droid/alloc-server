using AllocServer.Data;
using AllocServer.DTOs.Workspaces;
using AllocServer.Interfaces.Projects;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Project_Services
{
    public class ProjectService : IProjectService
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
        {
            "Planning",
            "In Progress",
            "Completed",
            "On Hold",
            "Cancelled"
        };

        private readonly ApplicationDbContext _context;

        public ProjectService(ApplicationDbContext context)
        {
            _context = context;
        }

        public ProjectDetailResponse GetProject(Project project)
        {
            return MapProject(project);
        }

        public async Task<ProjectDetailResponse> UpdateProjectAsync(
            int accountId,
            Project project,
            UpdateProjectRequest request)
        {
            var projectName = NormalizeOptionalString(request.ProjectName);
            if (projectName == null)
            {
                throw new ArgumentException("Ten du an khong duoc de trong.");
            }

            if (request.StartDate == null || request.EndDate == null)
            {
                throw new ArgumentException("Ngay bat dau va ngay ket thuc la bat buoc.");
            }

            if (request.EndDate.Value < request.StartDate.Value)
            {
                throw new ArgumentException("Ngay ket thuc phai lon hon hoac bang ngay bat dau.");
            }

            if (request.ExpectedBudget == null || request.ExpectedBudget < 0)
            {
                throw new ArgumentException("Ngan sach khong hop le.");
            }

            if (request.TotalRevenue == null || request.TotalRevenue < 0)
            {
                throw new ArgumentException("Doanh thu khong hop le.");
            }

            var status = NormalizeProjectStatus(request.Status);
            if (status == null)
            {
                throw new ArgumentException("Status chi nhan Planning, In Progress, Completed, On Hold hoac Cancelled.");
            }

            var isDuplicateName = await _context.Projects
                .AsNoTracking()
                .AnyAsync(item =>
                    item.WorkspaceID == project.WorkspaceID
                    && item.ProjectID != project.ProjectID
                    && item.ProjectName == projectName);

            if (isDuplicateName)
            {
                throw new InvalidOperationException("Ten du an da ton tai trong Workspace.");
            }

            project.ProjectName = projectName;
            project.ExpectedBudget = request.ExpectedBudget.Value;
            project.TotalRevenue = request.TotalRevenue.Value;
            project.StartDate = request.StartDate.Value;
            project.EndDate = request.EndDate.Value;
            project.Status = status;
            project.BaselineData = request.BaselineData;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                    && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    throw new InvalidOperationException("Ten du an da ton tai trong Workspace.");
                }

                throw;
            }

            return MapProject(project);
        }

        public async Task DeleteProjectAsync(int accountId, Project project)
        {
            var deletedAt = DateTime.UtcNow;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                project.IsDeleted = true;
                project.DeletedAt = deletedAt;
                project.DeletedBy = accountId;

                // Soft-delete indirect children before their parent tasks/conversations are hidden by query filters.
                await _context.Timesheets
                    .Where(item => _context.ProjectTasks
                        .Any(task => task.TaskID == item.TaskID && task.ProjectID == project.ProjectID))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.OTRequests
                    .Where(item => item.TaskID != null
                        && _context.ProjectTasks
                            .Any(task => task.TaskID == item.TaskID && task.ProjectID == project.ProjectID))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.TaskComments
                    .Where(item => _context.ProjectTasks
                        .Any(task => task.TaskID == item.TaskID && task.ProjectID == project.ProjectID))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.Messages
                    .Where(item => _context.Conversations
                        .Any(conversation =>
                            conversation.ConversationID == item.ConversationID
                            && conversation.ProjectID == project.ProjectID))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.ProjectTasks
                    .Where(item => item.ProjectID == project.ProjectID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.Expenses
                    .Where(item => item.ProjectID == project.ProjectID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.Revenues
                    .Where(item => item.ProjectID == project.ProjectID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.ProjectAssets
                    .Where(item => item.ProjectID == project.ProjectID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.Risks
                    .Where(item => item.ProjectID == project.ProjectID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.Conversations
                    .Where(item => item.ProjectID == project.ProjectID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.DeletedBy, accountId));

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static ProjectDetailResponse MapProject(Project project)
        {
            return new ProjectDetailResponse
            {
                ProjectID = project.ProjectID,
                WorkspaceID = project.WorkspaceID,
                ProjectName = project.ProjectName,
                ExpectedBudget = project.ExpectedBudget,
                TotalRevenue = project.TotalRevenue,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status,
                BaselineData = project.BaselineData,
                CreatedAt = project.CreatedAt
            };
        }

        private static string? NormalizeProjectStatus(string? status)
        {
            var normalized = NormalizeOptionalString(status);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "INPROGRESS" or "IN PROGRESS" => "In Progress",
                "ONHOLD" or "ON HOLD" => "On Hold",
                "PLANNING" => "Planning",
                "COMPLETED" => "Completed",
                "CANCELLED" => "Cancelled",
                _ => normalized
            };

            return AllowedStatuses.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
