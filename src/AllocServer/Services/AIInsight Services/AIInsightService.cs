using AllocServer.Data;
using AllocServer.DTOs.AIInsights;
using AllocServer.Interfaces.AIInsights;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.AIInsight_Services
{
    public class AIInsightService : IAIInsightService
    {
        private static readonly HashSet<string> AllowedSuggestionTypes = new(StringComparer.Ordinal)
        {
            "Risk Warning",
            "Resource Suggestion",
            "Budget Forecast"
        };

        private static readonly HashSet<string> AllowedFeedbackValues = new(StringComparer.Ordinal)
        {
            "Accepted",
            "Rejected",
            "Ignored"
        };

        private readonly ApplicationDbContext _context;

        public AIInsightService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedAIInsightsResponse> GetProjectAIInsightsAsync(
            Project project,
            GetProjectAIInsightsQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var suggestionType = NormalizeSuggestionType(query.SuggestionType);
            var userFeedback = NormalizeFeedback(query.UserFeedback);

            if (!string.IsNullOrWhiteSpace(query.SuggestionType) && suggestionType == null)
            {
                throw new ArgumentException(
                    "SuggestionType chi nhan Risk Warning, Resource Suggestion hoac Budget Forecast.");
            }

            if (!string.IsNullOrWhiteSpace(query.UserFeedback) && userFeedback == null)
            {
                throw new ArgumentException("UserFeedback chi nhan Accepted, Rejected hoac Ignored.");
            }

            var insightsQuery = _context.AILogs
                .AsNoTracking()
                .Where(log => log.ProjectID == project.ProjectID);

            if (suggestionType != null)
            {
                insightsQuery = insightsQuery.Where(log => log.SuggestionType == suggestionType);
            }

            if (userFeedback != null)
            {
                insightsQuery = insightsQuery.Where(log => log.UserFeedback == userFeedback);
            }

            var totalItems = await insightsQuery.CountAsync();
            var items = await insightsQuery
                .OrderByDescending(log => log.CreatedAt)
                .ThenByDescending(log => log.LogID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new AIInsightResponse
                {
                    LogId = log.LogID,
                    ProjectId = log.ProjectID,
                    SuggestionType = log.SuggestionType,
                    SuggestionContent = log.SuggestionContent,
                    UserFeedback = log.UserFeedback,
                    CorrectedRiskLevel = log.CorrectedRiskLevel,
                    IsVerified = log.IsVerified,
                    VerifiedBy = log.VerifiedBy,
                    VerifiedAt = log.VerifiedAt,
                    CreatedAt = log.CreatedAt
                })
                .ToListAsync();

            return new PagedAIInsightsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<AIInsightResponse> UpdateLogFeedbackAsync(
            int projectId,
            int logId,
            int accountId,
            UpdateAILogFeedbackRequest request)
        {
            var log = await _context.AILogs
                .FirstOrDefaultAsync(l => l.LogID == logId && l.ProjectID == projectId);

            if (log == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy AI Log với ID {logId} thuộc dự án {projectId}.");
            }

            if (!string.IsNullOrWhiteSpace(request.UserFeedback))
            {
                var normalizedFeedback = NormalizeFeedback(request.UserFeedback);
                if (normalizedFeedback == null)
                {
                    throw new ArgumentException("UserFeedback chỉ nhận các giá trị 'Accepted', 'Rejected' hoặc 'Ignored'.");
                }
                log.UserFeedback = normalizedFeedback;
            }

            if (request.CorrectedRiskLevel.HasValue)
            {
                if (request.CorrectedRiskLevel.Value < 0 || request.CorrectedRiskLevel.Value > 3)
                {
                    throw new ArgumentException("CorrectedRiskLevel phải nằm trong khoảng từ 0 (Low) đến 3 (Critical).");
                }
                log.CorrectedRiskLevel = request.CorrectedRiskLevel.Value;
            }

            if (request.IsVerified.HasValue)
            {
                log.IsVerified = request.IsVerified.Value;
                if (log.IsVerified)
                {
                    log.VerifiedBy = accountId;
                    log.VerifiedAt = DateTime.UtcNow;
                }
                else
                {
                    log.VerifiedBy = null;
                    log.VerifiedAt = null;
                }
            }
            else if (request.CorrectedRiskLevel.HasValue)
            {
                log.IsVerified = true;
                log.VerifiedBy = accountId;
                log.VerifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new AIInsightResponse
            {
                LogId = log.LogID,
                ProjectId = log.ProjectID,
                SuggestionType = log.SuggestionType,
                SuggestionContent = log.SuggestionContent,
                UserFeedback = log.UserFeedback,
                CorrectedRiskLevel = log.CorrectedRiskLevel,
                IsVerified = log.IsVerified,
                VerifiedBy = log.VerifiedBy,
                VerifiedAt = log.VerifiedAt,
                CreatedAt = log.CreatedAt
            };
        }

        private static string? NormalizeSuggestionType(string? value)
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

            return AllowedSuggestionTypes.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeFeedback(string? value)
        {
            var normalized = NormalizeOptionalString(value);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "ACCEPTED" => "Accepted",
                "REJECTED" => "Rejected",
                "IGNORED" => "Ignored",
                _ => normalized
            };

            return AllowedFeedbackValues.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
