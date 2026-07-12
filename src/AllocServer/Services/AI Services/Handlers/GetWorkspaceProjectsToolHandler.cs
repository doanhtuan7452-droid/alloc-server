using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AllocServer.Data;
using AllocServer.DTOs.AIChat;
using AllocServer.Interfaces.AI;
using AllocServer.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.AI_Services.Handlers
{
    public class GetWorkspaceProjectsToolHandler : IAIToolHandler
    {
        private readonly ApplicationDbContext _context;

        public GetWorkspaceProjectsToolHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public string ToolName => "get_workspace_projects";

        public async Task<AIToolResult> ExecuteAsync(Dictionary<string, object> arguments)
        {
            GetWorkspaceProjectsToolPayload payload;
            try
            {
                var json = JsonSerializer.Serialize(arguments);
                payload = JsonSerializer.Deserialize<GetWorkspaceProjectsToolPayload>(json)
                          ?? throw new ArgumentException("Không thể parse arguments sang GetWorkspaceProjectsToolPayload.");
            }
            catch (Exception ex)
            {
                return AIToolResult.Failure(400, "INVALID_PAYLOAD", $"Payload không hợp lệ: {ex.Message}");
            }

            if (payload.WorkspaceId <= 0)
            {
                return AIToolResult.Failure(400, "MISSING_WORKSPACE_ID", "Bắt buộc phải truyền workspaceId hợp lệ.");
            }

            // 1. Lọc bảng Projects trước để tối ưu hóa hiệu năng SQL
            var projectsQuery = _context.Projects
                .AsNoTracking()
                .Where(p => p.WorkspaceID == payload.WorkspaceId && !p.IsDeleted);

            var normalizedStatus = NormalizeProjectStatus(payload.Status);
            if (!string.IsNullOrEmpty(normalizedStatus))
            {
                projectsQuery = projectsQuery.Where(p => p.Status == normalizedStatus);
            }

            // 2. Tiến hành JOIN với ProjectRiskFeatures trên DB (vẫn giữ IQueryable)
            var jointQuery = from p in projectsQuery
                             join rf in _context.ProjectRiskFeatures.AsNoTracking() on p.ProjectID equals rf.ProjectID into rfGroup
                             from rf in rfGroup.DefaultIfEmpty()
                             select new
                             {
                                 Project = p,
                                 RiskFeature = rf
                             };

            // 3. Sắp xếp (Sorting) trực tiếp trên IQueryable
            switch (payload.SortBy?.ToLowerInvariant())
            {
                case "risk_score_desc":
                    jointQuery = jointQuery.OrderByDescending(x => (double?)x.RiskFeature.Overall_Risk_Score ?? 0.0);
                    break;
                case "budget_desc":
                    jointQuery = jointQuery.OrderByDescending(x => x.Project.ExpectedBudget);
                    break;
                case "newest":
                default:
                    jointQuery = jointQuery.OrderByDescending(x => x.Project.CreatedAt);
                    break;
            }

            // 4. Lấy tổng số lượng dự án phù hợp (để phân trang)
            var totalCount = await jointQuery.CountAsync();

            // 5. Phân trang (Pagination) trên IQueryable và chỉ execute câu SQL cuối cùng qua ToListAsync
            var pagedResults = await jointQuery
                .Skip(payload.Skip)
                .Take(payload.Limit)
                .ToListAsync();

            // 6. Map dữ liệu trả về cho Python LLM Server
            var projectList = pagedResults.Select(x => new
            {
                projectId = x.Project.ProjectID,
                projectName = x.Project.ProjectName,
                expectedBudget = x.Project.ExpectedBudget,
                totalRevenue = x.Project.TotalRevenue,
                startDate = x.Project.StartDate.ToString("yyyy-MM-dd"),
                endDate = x.Project.EndDate.ToString("yyyy-MM-dd"),
                status = x.Project.Status,
                methodology = x.Project.Methodology,
                originalCurrencyCode = x.Project.OriginalCurrencyCode,
                exchangeRateToUSD = x.Project.ExchangeRateToUSD,
                createdAt = x.Project.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                totalTasks = x.RiskFeature?.Total_Tasks ?? 0,
                teamSize = x.RiskFeature?.Team_Size ?? 0,
                avgTeamSkillLevel = x.RiskFeature?.Avg_Team_Skill_Level ?? 3.0,
                rawComplexityScore = x.RiskFeature?.Raw_Complexity_Score ?? 0.0,
                budgetUtilizationRate = x.RiskFeature?.Budget_Utilization_Rate ?? 0m,
                overallRiskScore = x.RiskFeature?.Overall_Risk_Score ?? 0.0
            }).ToList();

            return AIToolResult.Success(new
            {
                workspaceId = payload.WorkspaceId,
                total = totalCount,
                limit = payload.Limit,
                skip = payload.Skip,
                projects = projectList
            });
        }

        private static string? NormalizeProjectStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            var normalized = status.Trim().ToUpperInvariant();
            return normalized switch
            {
                "INPROGRESS" or "IN PROGRESS" => "In Progress",
                "ONHOLD" or "ON HOLD" => "On Hold",
                "PLANNING" => "Planning",
                "COMPLETED" => "Completed",
                "CANCELLED" => "Cancelled",
                _ => status.Trim()
            };
        }
    }
}
