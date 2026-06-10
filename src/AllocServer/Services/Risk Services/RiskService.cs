using AllocServer.Data;
using AllocServer.DTOs.Risks;
using AllocServer.Interfaces.Risks;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Risk_Services
{
    public class RiskService : IRiskService
    {
        private const decimal MaxMoneyAmount = 9999999999999999.99m;

        private static readonly HashSet<string> AllowedCategories = new(StringComparer.Ordinal)
        {
            "Schedule",
            "Financial",
            "Resource",
            "Technical",
            "External"
        };

        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
        {
            "Identified",
            "Assessed",
            "Mitigation Planned",
            "In Progress",
            "Realized",
            "Closed"
        };

        private static readonly HashSet<string> AllowedMitigationStrategies = new(StringComparer.Ordinal)
        {
            "Avoid",
            "Transfer",
            "Mitigate",
            "Accept"
        };

        private static readonly HashSet<string> AllowedMitigationStatuses = new(StringComparer.Ordinal)
        {
            "Planned",
            "In Progress",
            "Completed",
            "Failed"
        };

        private readonly ApplicationDbContext _context;

        public RiskService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedProjectRisksResponse> GetProjectRisksAsync(
            Project project,
            GetProjectRisksQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var search = NormalizeOptionalString(query.Search);
            var category = NormalizeCategory(query.Category);
            var status = NormalizeStatus(query.Status, allowDefault: false);

            if (!string.IsNullOrWhiteSpace(query.Category) && category == null)
            {
                throw new ArgumentException(
                    "Category chi nhan Schedule, Financial, Resource, Technical hoac External.");
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && status == null)
            {
                throw new ArgumentException(
                    "Status chi nhan Identified, Assessed, Mitigation Planned, In Progress, Realized hoac Closed.");
            }

            ValidateScoreRange(query.MinScore, query.MaxScore);

            var risksQuery = _context.Risks
                .AsNoTracking()
                .Where(item => item.ProjectID == project.ProjectID);

            if (search != null)
            {
                risksQuery = risksQuery.Where(item =>
                    item.RiskName.Contains(search)
                    || (item.Description != null && item.Description.Contains(search)));
            }

            if (category != null)
            {
                risksQuery = risksQuery.Where(item => item.Category == category);
            }

            if (status != null)
            {
                risksQuery = risksQuery.Where(item => item.Status == status);
            }

            if (query.TaskId != null)
            {
                risksQuery = risksQuery.Where(item => item.TaskID == query.TaskId.Value);
            }

            if (query.OwnerId != null)
            {
                risksQuery = risksQuery.Where(item => item.OwnerID == query.OwnerId.Value);
            }

            if (query.MinScore != null)
            {
                risksQuery = risksQuery.Where(item => item.RiskScore >= query.MinScore.Value);
            }

            if (query.MaxScore != null)
            {
                risksQuery = risksQuery.Where(item => item.RiskScore <= query.MaxScore.Value);
            }

            var totalItems = await risksQuery.CountAsync();
            var items = await risksQuery
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.RiskID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new RiskListItemResponse
                {
                    RiskId = item.RiskID,
                    ProjectId = item.ProjectID,
                    ProjectName = project.ProjectName,
                    TaskId = item.TaskID,
                    RiskName = item.RiskName,
                    Description = item.Description,
                    Category = item.Category,
                    Probability = item.Probability,
                    Impact = item.Impact,
                    RiskScore = item.RiskScore,
                    EstimatedFinancialImpact = item.EstimatedFinancialImpact,
                    ActualFinancialImpact = item.ActualFinancialImpact,
                    Status = item.Status,
                    OwnerId = item.OwnerID,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                })
                .ToListAsync();

            return new PagedProjectRisksResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<RiskDetailResponse> CreateProjectRiskAsync(
            int accountId,
            Project project,
            CreateRiskRequest request)
        {
            // Validate RiskName
            var riskName = NormalizeOptionalString(request.RiskName);
            if (riskName == null)
            {
                throw new ArgumentException("RiskNameRequired");
            }

            if (riskName.Length > 255)
            {
                throw new ArgumentException("RiskNameMaxLength");
            }

            // Validate Category
            string? category = null;
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                category = NormalizeCategory(request.Category);
                if (category == null)
                {
                    throw new ArgumentException(
                        "Category chi nhan Schedule, Financial, Resource, Technical hoac External.");
                }
            }

            // Validate Probability & Impact
            if (request.Probability < 1 || request.Probability > 5)
            {
                throw new ArgumentException("InvalidRiskProbability");
            }

            if (request.Impact < 1 || request.Impact > 5)
            {
                throw new ArgumentException("InvalidRiskImpact");
            }

            // Validate Status
            var status = NormalizeStatus(request.Status, allowDefault: true);
            if (status == null)
            {
                throw new ArgumentException(
                    "Status chi nhan Identified, Assessed, Mitigation Planned, In Progress, Realized hoac Closed.");
            }

            // Validate EstimatedFinancialImpact
            if (request.EstimatedFinancialImpact < 0 || request.EstimatedFinancialImpact > MaxMoneyAmount)
            {
                throw new ArgumentException("InvalidEstimatedFinancialImpact");
            }

            // Validate TaskID — must belong to same project and not soft-deleted
            if (request.TaskId != null)
            {
                var taskExists = await _context.ProjectTasks
                    .AsNoTracking()
                    .AnyAsync(task =>
                        task.TaskID == request.TaskId.Value
                        && task.ProjectID == project.ProjectID);

                if (!taskExists)
                {
                    throw new ArgumentException(
                        "Task khong ton tai hoac khong thuoc project nay.");
                }
            }

            // Validate OwnerID — must be active workspace member in project's workspace
            if (request.OwnerId != null)
            {
                var isValidOwner = await _context.WorkspaceMembers
                    .AsNoTracking()
                    .AnyAsync(member =>
                        member.WorkspaceMemberID == request.OwnerId.Value
                        && member.WorkspaceID == project.WorkspaceID
                        && member.Status == "Active"
                        && !member.Workspace.IsDeleted
                        && !member.Resource.IsDeleted
                        && _context.Accounts.Any(account =>
                            account.AccountID == member.Resource.AccountID));

                if (!isValidOwner)
                {
                    throw new ArgumentException(
                        "Owner khong ton tai, khong active hoac khong thuoc workspace cua project.");
                }
            }

            var risk = new Risk
            {
                ProjectID = project.ProjectID,
                TaskID = request.TaskId,
                RiskName = riskName,
                Description = NormalizeNullableText(request.Description),
                Category = category,
                Probability = request.Probability,
                Impact = request.Impact,
                EstimatedFinancialImpact = request.EstimatedFinancialImpact,
                Status = status,
                OwnerID = request.OwnerId
            };

            _context.Risks.Add(risk);
            await _context.SaveChangesAsync();

            // Reload entity to get computed column RiskScore from database
            await _context.Entry(risk).ReloadAsync();

            return MapRisk(risk, project.ProjectName);
        }

        private static void ValidateScoreRange(int? minScore, int? maxScore)
        {
            if (minScore is < 1 || minScore is > 25)
            {
                throw new ArgumentException("InvalidMinScore");
            }

            if (maxScore is < 1 || maxScore is > 25)
            {
                throw new ArgumentException("InvalidMaxScore");
            }

            if (minScore != null && maxScore != null && maxScore < minScore)
            {
                throw new ArgumentException("InvalidScoreRange");
            }
        }

        private static string? NormalizeCategory(string? category)
        {
            var normalized = NormalizeOptionalString(category);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "SCHEDULE" => "Schedule",
                "FINANCIAL" => "Financial",
                "RESOURCE" => "Resource",
                "TECHNICAL" => "Technical",
                "EXTERNAL" => "External",
                _ => normalized
            };

            return AllowedCategories.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeStatus(string? status, bool allowDefault)
        {
            var normalized = NormalizeOptionalString(status);
            if (normalized == null)
                return allowDefault ? "Identified" : null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "IDENTIFIED" => "Identified",
                "ASSESSED" => "Assessed",
                "MITIGATION PLANNED" => "Mitigation Planned",
                "IN PROGRESS" => "In Progress",
                "REALIZED" => "Realized",
                "CLOSED" => "Closed",
                _ => normalized
            };

            return AllowedStatuses.Contains(candidate) ? candidate : null;
        }

        private static RiskDetailResponse MapRisk(Risk risk, string projectName)
        {
            return new RiskDetailResponse
            {
                RiskId = risk.RiskID,
                ProjectId = risk.ProjectID,
                ProjectName = projectName,
                TaskId = risk.TaskID,
                RiskName = risk.RiskName,
                Description = risk.Description,
                Category = risk.Category,
                Probability = risk.Probability,
                Impact = risk.Impact,
                RiskScore = risk.RiskScore,
                EstimatedFinancialImpact = risk.EstimatedFinancialImpact,
                ActualFinancialImpact = risk.ActualFinancialImpact,
                Status = risk.Status,
                OwnerId = risk.OwnerID,
                CreatedAt = risk.CreatedAt,
                UpdatedAt = risk.UpdatedAt
            };
        }

        public async Task<RiskMitigationResponse> CreateRiskMitigationAsync(
            int accountId,
            Risk risk,
            CreateRiskMitigationRequest request)
        {
            // Validate StrategyType
            var strategyType = NormalizeMitigationStrategy(request.StrategyType);
            if (strategyType == null)
            {
                throw new ArgumentException(
                    "StrategyType chi nhan Avoid, Transfer, Mitigate hoac Accept.");
            }

            // Validate ActionPlan
            var actionPlan = NormalizeOptionalString(request.ActionPlan);
            if (actionPlan == null)
            {
                throw new ArgumentException("ActionPlanRequired");
            }

            // Validate MitigationCost
            if (request.MitigationCost < 0 || request.MitigationCost > MaxMoneyAmount)
            {
                throw new ArgumentException(
                    "MitigationCost phai tu 0 den 9999999999999999.99.");
            }

            // Validate Status
            var mitigationStatus = NormalizeMitigationStatus(request.Status, allowDefault: true);
            if (mitigationStatus == null)
            {
                throw new ArgumentException(
                    "Status chi nhan Planned, In Progress, Completed hoac Failed.");
            }

            // Check Risk status — cannot create mitigation for Realized or Closed
            if (risk.Status == "Realized" || risk.Status == "Closed")
            {
                throw new InvalidOperationException(
                    "Khong the tao mitigation cho risk da Realized hoac Closed.");
            }

            // Validate AssignedMemberId
            if (request.AssignedMemberId != null)
            {
                var isValidMember = await _context.WorkspaceMembers
                    .AsNoTracking()
                    .AnyAsync(member =>
                        member.WorkspaceMemberID == request.AssignedMemberId.Value
                        && member.WorkspaceID == risk.Project!.WorkspaceID
                        && member.Status == "Active"
                        && !member.Workspace.IsDeleted
                        && !member.Resource.IsDeleted
                        && _context.Accounts.Any(account =>
                            account.AccountID == member.Resource.AccountID));

                if (!isValidMember)
                {
                    throw new ArgumentException(
                        "AssignedMember khong ton tai, khong active hoac khong thuoc workspace cua risk.");
                }
            }

            // Validate TargetDate
            if (request.TargetDate != null)
            {
                if (request.TargetDate.Value < risk.Project!.StartDate
                    || request.TargetDate.Value > risk.Project.EndDate)
                {
                    throw new ArgumentException(
                        "TargetDate phai nam trong khoang StartDate va EndDate cua project.");
                }
            }

            // Resolve actorMemberId from accountId
            var actorMemberId = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.Resource.AccountID == accountId
                    && member.WorkspaceID == risk.Project!.WorkspaceID
                    && member.Status == "Active")
                .Select(member => member.WorkspaceMemberID)
                .FirstOrDefaultAsync();

            if (actorMemberId == 0)
            {
                throw new UnauthorizedAccessException(
                    "Khong tim thay thanh vien active trong workspace.");
            }

            // Transaction: create mitigation + optional status transition
            var mitigation = new RiskMitigation
            {
                RiskID = risk.RiskID,
                StrategyType = strategyType,
                ActionPlan = actionPlan,
                MitigationCost = request.MitigationCost,
                AssignedMemberID = request.AssignedMemberId,
                TargetDate = request.TargetDate,
                Status = mitigationStatus
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.RiskMitigations.Add(mitigation);

                // Status transition: Identified/Assessed → Mitigation Planned
                if (risk.Status == "Identified" || risk.Status == "Assessed")
                {
                    var oldStatus = risk.Status;
                    risk.Status = "Mitigation Planned";
                    risk.UpdatedAt = DateTime.UtcNow;

                    _context.RiskLifecycles.Add(new RiskLifecycle
                    {
                        RiskID = risk.RiskID,
                        ChangedByMemberID = actorMemberId,
                        OldStatus = oldStatus,
                        NewStatus = "Mitigation Planned",
                        OldScore = risk.RiskScore,
                        NewScore = risk.RiskScore,
                        ChangeNote = "Da lap ke hoach giam thieu rui ro."
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return MapMitigation(mitigation);
        }

        public async Task<List<RiskLifecycleResponse>> GetRiskLifecycleAsync(Risk risk)
        {
            return await _context.RiskLifecycles
                .AsNoTracking()
                .Where(lifecycle => lifecycle.RiskID == risk.RiskID)
                .OrderBy(lifecycle => lifecycle.ChangeDate)
                .ThenBy(lifecycle => lifecycle.HistoryID)
                .Take(500)
                .Select(lifecycle => new RiskLifecycleResponse
                {
                    HistoryId = lifecycle.HistoryID,
                    RiskId = lifecycle.RiskID,
                    ChangedByMemberId = lifecycle.ChangedByMemberID,
                    OldStatus = lifecycle.OldStatus,
                    NewStatus = lifecycle.NewStatus,
                    OldScore = lifecycle.OldScore,
                    NewScore = lifecycle.NewScore,
                    ChangeNote = lifecycle.ChangeNote,
                    ChangeDate = lifecycle.ChangeDate
                })
                .ToListAsync();
        }

        private static RiskMitigationResponse MapMitigation(RiskMitigation mitigation)
        {
            return new RiskMitigationResponse
            {
                MitigationId = mitigation.MitigationID,
                RiskId = mitigation.RiskID,
                StrategyType = mitigation.StrategyType,
                ActionPlan = mitigation.ActionPlan,
                MitigationCost = mitigation.MitigationCost,
                AssignedMemberId = mitigation.AssignedMemberID,
                TargetDate = mitigation.TargetDate,
                Status = mitigation.Status,
                CreatedAt = mitigation.CreatedAt
            };
        }

        private static string? NormalizeMitigationStrategy(string? strategy)
        {
            var normalized = NormalizeOptionalString(strategy);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "AVOID" => "Avoid",
                "TRANSFER" => "Transfer",
                "MITIGATE" => "Mitigate",
                "ACCEPT" => "Accept",
                _ => normalized
            };

            return AllowedMitigationStrategies.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeMitigationStatus(string? status, bool allowDefault)
        {
            var normalized = NormalizeOptionalString(status);
            if (normalized == null)
                return allowDefault ? "Planned" : null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "PLANNED" => "Planned",
                "IN PROGRESS" => "In Progress",
                "COMPLETED" => "Completed",
                "FAILED" => "Failed",
                _ => normalized
            };

            return AllowedMitigationStatuses.Contains(candidate) ? candidate : null;
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
    }
}
