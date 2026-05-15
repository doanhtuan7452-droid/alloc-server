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
                throw new ArgumentException("RiskName la bat buoc.");
            }

            if (riskName.Length > 255)
            {
                throw new ArgumentException("RiskName toi da 255 ky tu.");
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
                throw new ArgumentException("Probability phai tu 1 den 5.");
            }

            if (request.Impact < 1 || request.Impact > 5)
            {
                throw new ArgumentException("Impact phai tu 1 den 5.");
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
                throw new ArgumentException("EstimatedFinancialImpact phai tu 0 den 9999999999999999.99.");
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
                throw new ArgumentException("minScore phai tu 1 den 25.");
            }

            if (maxScore is < 1 || maxScore is > 25)
            {
                throw new ArgumentException("maxScore phai tu 1 den 25.");
            }

            if (minScore != null && maxScore != null && maxScore < minScore)
            {
                throw new ArgumentException("maxScore phai lon hon hoac bang minScore.");
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
