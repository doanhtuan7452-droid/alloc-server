using AllocServer.Data;
using AllocServer.DTOs.AIInsights;
using AllocServer.Exceptions;
using AllocServer.Filters;
using AllocServer.Interfaces;
using AllocServer.Interfaces.AI;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.AI_Services
{
    public class AIAnalysisService : IAIAnalysisService
    {
        private const string AIChatQuotaFeatureCode = "AI_CHAT_QUOTA";
        private const string AIRiskManagementFeatureCode = "AI_RISK_MGT";

        private static readonly HashSet<string> AllowedAnalysisTypes = new(StringComparer.Ordinal)
        {
            "Risk Warning",
            "Resource Suggestion",
            "Budget Forecast"
        };

        private readonly ApplicationDbContext _context;
        private readonly IFeatureQuotaService _featureQuotaService;
        private readonly IAIProvider _aiProvider;
        private readonly IConfiguration _configuration;

        public AIAnalysisService(
            ApplicationDbContext context,
            IFeatureQuotaService featureQuotaService,
            IAIProvider aiProvider,
            IConfiguration configuration)
        {
            _context = context;
            _featureQuotaService = featureQuotaService;
            _aiProvider = aiProvider;
            _configuration = configuration;
        }

        public async Task<AIAskResponse> AnalyzeAsync(int accountId, AIAskRequest request)
        {
            var analysisType = NormalizeAnalysisType(request.AnalysisType);
            if (analysisType == null)
            {
                throw new ArgumentException(
                    "AnalysisType chi nhan Risk Warning, Resource Suggestion hoac Budget Forecast.");
            }

            var project = await LoadProjectAsync(request.ProjectId);
            var membership = await ResolveActiveMembershipAsync(accountId, project.WorkspaceID);
            await EnsureAskPermissionAsync(membership);

            if (analysisType == "Risk Warning")
            {
                await EnsureRiskAiFeatureEnabledAsync(project.WorkspaceID);
            }

            var targetEntitySummary = await BuildTargetEntitySummaryAsync(project.ProjectID, request.TargetEntityId);
            var context = new AIAnalysisContext
            {
                AnalysisType = analysisType,
                Prompt = NormalizeNullableText(request.Prompt),
                ProjectSummary = await BuildProjectSummaryAsync(project),
                TargetEntitySummary = targetEntitySummary
            };

            var billingMonth = GetCurrentBillingMonth();
            var effectiveLimit = await ResolveEffectiveQuotaLimitAsync(project.WorkspaceID);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await EnsureMonthlyUsageRowAsync(project.WorkspaceID, billingMonth);

                var newCount = await TryConsumeAIQuotaAsync(
                    project.WorkspaceID,
                    billingMonth,
                    effectiveLimit);

                var content = await _aiProvider.GenerateAnalysisAsync(context);
                var now = DateTime.UtcNow;
                var log = new AILog
                {
                    ProjectID = project.ProjectID,
                    SuggestionType = analysisType,
                    SuggestionContent = content,
                    CreatedAt = now
                };

                _context.AILogs.Add(log);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new AIAskResponse
                {
                    LogId = log.LogID,
                    ProjectId = log.ProjectID,
                    AnalysisType = log.SuggestionType,
                    Content = log.SuggestionContent,
                    CreatedAt = log.CreatedAt,
                    RemainingQuota = CalculateRemainingQuota(effectiveLimit, newCount)
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<Project> LoadProjectAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(item => item.Workspace)
                .FirstOrDefaultAsync(item =>
                    item.ProjectID == projectId
                    && item.Workspace != null
                    && !item.Workspace.IsDeleted);

            if (project == null)
            {
                throw new KeyNotFoundException("Khong tim thay Project.");
            }

            return project;
        }

        private async Task<ActiveMembership> ResolveActiveMembershipAsync(
            int accountId,
            int workspaceId)
        {
            var membership = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.WorkspaceID == workspaceId
                    && member.Resource.AccountID == accountId
                    && member.Status == "Active"
                    && !member.Workspace.IsDeleted
                    && !member.Resource.IsDeleted)
                .Select(member => new ActiveMembership
                {
                    WorkspaceMemberID = member.WorkspaceMemberID,
                    WorkspaceRoleID = member.WorkspaceRoleID,
                    RoleName = member.WorkspaceRole.RoleName
                })
                .FirstOrDefaultAsync();

            if (membership == null)
            {
                throw new UnauthorizedAccessException(
                    "Ban khong phai thanh vien active cua workspace chua Project nay.");
            }

            return membership;
        }

        private async Task EnsureAskPermissionAsync(ActiveMembership membership)
        {
            if (string.Equals(membership.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var hasPermission = await _context.RolePermissions
                .AsNoTracking()
                .AnyAsync(rolePermission =>
                    rolePermission.WorkspaceRoleID == membership.WorkspaceRoleID
                    && rolePermission.PermissionID == AIPermissionIds.Ask);

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Ban khong co quyen su dung AI trong workspace nay.");
            }
        }

        private async Task EnsureRiskAiFeatureEnabledAsync(int workspaceId)
        {
            var hasFeature = await _featureQuotaService.CheckFeatureQuotaAsync(
                workspaceId,
                AIRiskManagementFeatureCode);

            if (!hasFeature)
            {
                throw new QuotaExceededException(
                    "Goi cuoc hien tai chua ho tro tinh nang Quan tri rui ro AI.",
                    "FEATURE_NOT_INCLUDED");
            }
        }

        private async Task<int> ResolveEffectiveQuotaLimitAsync(int workspaceId)
        {
            var isStrictCheckEnabled = _configuration.GetValue<bool>("FeatureToggles:EnableStrictQuotaCheck");
            if (!isStrictCheckEnabled)
            {
                return -1;
            }

            var limit = await _featureQuotaService.GetFeatureLimitAsync(
                workspaceId,
                AIChatQuotaFeatureCode);

            if (limit == null)
            {
                throw new QuotaExceededException("Khong tim thay cau hinh quota AI cho workspace nay.");
            }

            if (limit.LimitValue == -1)
            {
                return -1;
            }

            if (limit.LimitValue <= 0)
            {
                throw new QuotaExceededException("Workspace da het quota hoi dap AI cua goi cuoc hien tai.");
            }

            return limit.LimitValue;
        }

        private async Task<string> BuildProjectSummaryAsync(Project project)
        {
            var taskCount = await _context.ProjectTasks
                .AsNoTracking()
                .CountAsync(task => task.ProjectID == project.ProjectID);

            var openRiskCount = await _context.Risks
                .AsNoTracking()
                .CountAsync(risk =>
                    risk.ProjectID == project.ProjectID
                    && risk.Status != "Closed");

            return string.Join(
                "; ",
                $"ProjectID={project.ProjectID}",
                $"Name={project.ProjectName}",
                $"Status={project.Status}",
                $"ExpectedBudget={project.ExpectedBudget}",
                $"TotalRevenue={project.TotalRevenue}",
                $"StartDate={project.StartDate:yyyy-MM-dd}",
                $"EndDate={project.EndDate:yyyy-MM-dd}",
                $"TaskCount={taskCount}",
                $"OpenRiskCount={openRiskCount}");
        }

        private async Task<string?> BuildTargetEntitySummaryAsync(int projectId, int? targetEntityId)
        {
            if (targetEntityId == null)
            {
                return null;
            }

            var task = await _context.ProjectTasks
                .AsNoTracking()
                .Where(item =>
                    item.TaskID == targetEntityId.Value
                    && item.ProjectID == projectId)
                .Select(item => new
                {
                    item.TaskID,
                    item.TaskName,
                    item.Status,
                    item.DurationType,
                    item.EstimatedValue,
                    item.StartDate,
                    item.EndDate
                })
                .FirstOrDefaultAsync();

            if (task == null)
            {
                throw new ArgumentException("targetEntityId khong ton tai hoac khong thuoc Project nay.");
            }

            return string.Join(
                "; ",
                $"TaskID={task.TaskID}",
                $"TaskName={task.TaskName}",
                $"Status={task.Status}",
                $"DurationType={task.DurationType}",
                $"EstimatedValue={task.EstimatedValue}",
                $"StartDate={task.StartDate:yyyy-MM-dd}",
                $"EndDate={task.EndDate:yyyy-MM-dd}");
        }

        private async Task EnsureMonthlyUsageRowAsync(int workspaceId, DateOnly billingMonth)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (
    SELECT 1
    FROM WorkspaceMonthlyUsages WITH (UPDLOCK, HOLDLOCK)
    WHERE WorkspaceID = {workspaceId} AND BillingMonth = {billingMonth}
)
BEGIN
    INSERT INTO WorkspaceMonthlyUsages (WorkspaceID, BillingMonth, AIQueryCount, StorageUsedMB, UpdatedAt)
    VALUES ({workspaceId}, {billingMonth}, 0, 0, SYSUTCDATETIME())
END");
        }

        private async Task<int> TryConsumeAIQuotaAsync(
            int workspaceId,
            DateOnly billingMonth,
            int effectiveLimit)
        {
            var affectedRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE WorkspaceMonthlyUsages
SET AIQueryCount = ISNULL(AIQueryCount, 0) + 1,
    UpdatedAt = SYSUTCDATETIME()
WHERE WorkspaceID = {workspaceId}
  AND BillingMonth = {billingMonth}
  AND ({effectiveLimit} = -1 OR ISNULL(AIQueryCount, 0) < {effectiveLimit})");

            if (affectedRows == 0)
            {
                throw new QuotaExceededException("Workspace da vuot quota hoi dap AI cua thang hien tai.");
            }

            return await _context.WorkspaceMonthlyUsages
                .AsNoTracking()
                .Where(usage =>
                    usage.WorkspaceID == workspaceId
                    && usage.BillingMonth == billingMonth)
                .Select(usage => usage.AIQueryCount)
                .SingleAsync();
        }

        private static int? CalculateRemainingQuota(int effectiveLimit, int newCount)
        {
            return effectiveLimit == -1
                ? null
                : Math.Max(effectiveLimit - newCount, 0);
        }

        private static DateOnly GetCurrentBillingMonth()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return new DateOnly(today.Year, today.Month, 1);
        }

        private static string? NormalizeAnalysisType(string? value)
        {
            var normalized = NormalizeOptionalString(value);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "RISK" or "RISK WARNING" or "RISK_WARNING" or "RISK-WARNING" => "Risk Warning",
                "RESOURCE" or "RESOURCE SUGGESTION" or "RESOURCE_SUGGESTION" or "RESOURCE-SUGGESTION" => "Resource Suggestion",
                "BUDGET" or "BUDGET FORECAST" or "BUDGET_FORECAST" or "BUDGET-FORECAST" => "Budget Forecast",
                _ => normalized
            };

            return AllowedAnalysisTypes.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string? NormalizeNullableText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private sealed class ActiveMembership
        {
            public int WorkspaceMemberID { get; set; }
            public int WorkspaceRoleID { get; set; }
            public string RoleName { get; set; } = string.Empty;
        }
    }
}
