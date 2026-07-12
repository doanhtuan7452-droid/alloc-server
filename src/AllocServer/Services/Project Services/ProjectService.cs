using AllocServer.Data;
using AllocServer.DTOs.Workspaces;
using AllocServer.DTOs.Projects;
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

        public async Task<ProjectDetailResponse> GetProjectAsync(int projectId)
        {
            var projectAndStats = await (
                from p in _context.Projects
                where p.ProjectID == projectId && !p.IsDeleted
                join s in _context.ProjectProgressStats on p.ProjectID equals s.ProjectID into statsGroup
                from pgStat in statsGroup.DefaultIfEmpty()
                select new { Project = p, Stat = pgStat }
            ).FirstOrDefaultAsync();

            if (projectAndStats == null)
            {
                throw new KeyNotFoundException("ProjectNotFound");
            }

            var response = MapProject(projectAndStats.Project);
            response.Progress = projectAndStats.Stat != null ? projectAndStats.Stat.WeightedProgress : 0.0;
            return response;
        }

        public async Task<ProjectDetailResponse> UpdateProjectAsync(
            int accountId,
            int projectId,
            UpdateProjectRequest request)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectID == projectId && !p.IsDeleted);

            if (project == null)
            {
                throw new KeyNotFoundException("ProjectNotFound");
            }
            var projectName = NormalizeOptionalString(request.ProjectName);
            if (projectName == null)
            {
                throw new ArgumentException("ProjectNameRequired");
            }

            if (request.StartDate == null || request.EndDate == null)
            {
                throw new ArgumentException("StartEndDateRequired");
            }

            if (request.EndDate.Value < request.StartDate.Value)
            {
                throw new ArgumentException("EndDateBeforeStartDate");
            }

            if (request.ExpectedBudget == null || request.ExpectedBudget < 0)
            {
                throw new ArgumentException("InvalidBudget");
            }

            if (request.TotalRevenue == null || request.TotalRevenue < 0)
            {
                throw new ArgumentException("InvalidRevenue");
            }

            var status = NormalizeProjectStatus(request.Status);
            if (status == null)
            {
                throw new ArgumentException("InvalidProjectStatus");
            }

            var methodology = project.Methodology;
            if (request.Methodology != null)
            {
                var reqMethodology = NormalizeOptionalString(request.Methodology);
                if (reqMethodology == null || !IsAllowedMethodology(reqMethodology))
                {
                    throw new ArgumentException("InvalidMethodology");
                }
                methodology = reqMethodology;
            }

            var currencyCode = project.OriginalCurrencyCode;
            if (request.OriginalCurrencyCode != null)
            {
                var reqCurrencyCode = NormalizeOptionalString(request.OriginalCurrencyCode)?.ToUpperInvariant();
                if (string.IsNullOrEmpty(reqCurrencyCode) || reqCurrencyCode.Length > 5)
                {
                    throw new ArgumentException("InvalidOriginalCurrencyCode");
                }
                currencyCode = reqCurrencyCode;
            }

            var exchangeRate = project.ExchangeRateToUSD;
            if (request.ExchangeRateToUSD.HasValue)
            {
                if (request.ExchangeRateToUSD.Value <= 0 || request.ExchangeRateToUSD.Value > 999999.9999m)
                {
                    throw new ArgumentException("InvalidExchangeRate");
                }
                exchangeRate = request.ExchangeRateToUSD.Value;
            }

            var isDuplicateName = await _context.Projects
                .AsNoTracking()
                .AnyAsync(item =>
                    item.WorkspaceID == project.WorkspaceID
                    && item.ProjectID != project.ProjectID
                    && item.ProjectName == projectName);

            if (isDuplicateName)
            {
                throw new InvalidOperationException("ProjectNameExists");
            }

            project.ProjectName = projectName;
            project.ExpectedBudget = request.ExpectedBudget.Value;
            project.TotalRevenue = request.TotalRevenue.Value;
            project.StartDate = request.StartDate.Value;
            project.EndDate = request.EndDate.Value;
            project.Status = status;
            project.OriginalCurrencyCode = currencyCode;
            project.ExchangeRateToUSD = exchangeRate;
            project.Methodology = methodology;
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
                    throw new InvalidOperationException("ProjectNameExists");
                }

                throw;
            }

            var progressStat = await _context.ProjectProgressStats
                .FirstOrDefaultAsync(s => s.ProjectID == project.ProjectID);

            var response = MapProject(project);
            response.Progress = progressStat != null ? progressStat.WeightedProgress : 0.0;
            return response;
        }

        public async Task DeleteProjectAsync(int accountId, int projectId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectID == projectId && !p.IsDeleted);

            if (project == null)
            {
                throw new KeyNotFoundException("ProjectNotFound");
            }
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

        public async Task<ProjectProgressResponse> GetProjectProgressAsync(int projectId)
        {
            var projectAndStats = await (
                from p in _context.Projects
                where p.ProjectID == projectId && !p.IsDeleted
                join s in _context.ProjectProgressStats on p.ProjectID equals s.ProjectID into statsGroup
                from pgStat in statsGroup.DefaultIfEmpty()
                select new { Project = p, Stat = pgStat }
            ).FirstOrDefaultAsync();

            if (projectAndStats == null)
            {
                throw new KeyNotFoundException("ProjectNotFound");
            }

            var project = projectAndStats.Project;
            var stat = projectAndStats.Stat;

            return new ProjectProgressResponse
            {
                ProjectID = project.ProjectID,
                ProjectName = project.ProjectName,
                Status = project.Status,
                SimpleProgress = stat != null ? Math.Round(stat.SimpleProgress, 2) : 0.0,
                WeightedProgress = stat != null ? Math.Round(stat.WeightedProgress, 2) : 0.0,
                TotalTasks = stat != null ? stat.TotalTasks : 0,
                TodoTasks = stat != null ? stat.TodoCount : 0,
                InProgressTasks = stat != null ? stat.InProgressCount : 0,
                ReviewTasks = stat != null ? stat.ReviewCount : 0,
                DoneTasks = stat != null ? stat.DoneCount : 0
            };
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
                OriginalCurrencyCode = project.OriginalCurrencyCode,
                ExchangeRateToUSD = project.ExchangeRateToUSD,
                Methodology = project.Methodology,
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

        private static bool IsAllowedMethodology(string methodology)
        {
            return methodology is "Agile" or "Waterfall" or "Scrum" or "Kanban" or "Hybrid";
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
