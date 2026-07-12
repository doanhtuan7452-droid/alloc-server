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
    public class GetProjectInfoToolHandler : IAIToolHandler
    {
        private readonly ApplicationDbContext _context;

        public GetProjectInfoToolHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public string ToolName => "get_project_info";

        public async Task<AIToolResult> ExecuteAsync(Dictionary<string, object> arguments)
        {
            GetProjectInfoToolPayload payload;
            try
            {
                var json = JsonSerializer.Serialize(arguments);
                payload = JsonSerializer.Deserialize<GetProjectInfoToolPayload>(json)
                          ?? throw new ArgumentException("Không thể parse arguments sang GetProjectInfoToolPayload.");
            }
            catch (Exception ex)
            {
                return AIToolResult.Failure(400, "INVALID_PAYLOAD", $"Payload không hợp lệ: {ex.Message}");
            }

            // Truy vấn trực tiếp từ View vw_ProjectRiskFeatures đã được tối ưu hóa ở tầng DB
            var projectFeatures = await _context.ProjectRiskFeatures
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectID == payload.ProjectId);

            if (projectFeatures == null)
            {
                return AIToolResult.Failure(404, "PROJECT_NOT_FOUND", $"Không tìm thấy dự án có ID {payload.ProjectId} hoặc dự án đã bị xóa.");
            }

            var project = await _context.Projects
                .AsNoTracking()
                .FirstAsync(p => p.ProjectID == payload.ProjectId);

            // Truy vấn nhanh các task để đếm phân phối trạng thái (nếu rỗng sẽ trả về Dictionary rỗng {})
            var taskStatusStats = await _context.ProjectTasks
                .AsNoTracking()
                .Where(t => t.ProjectID == payload.ProjectId && !t.IsDeleted)
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            var result = new
            {
                projectId = projectFeatures.ProjectID,
                workspaceId = project.WorkspaceID,
                projectName = projectFeatures.Project_Name,
                expectedBudget = projectFeatures.Expected_Budget,
                totalRevenue = project.TotalRevenue,
                startDate = project.StartDate.ToString("yyyy-MM-dd"),
                endDate = project.EndDate.ToString("yyyy-MM-dd"),
                status = project.Status,
                methodology = projectFeatures.Methodology_Used,
                originalCurrencyCode = project.OriginalCurrencyCode,
                exchangeRateToUSD = project.ExchangeRateToUSD,
                totalTasksCount = projectFeatures.Total_Tasks,
                tasksByStatus = taskStatusStats,
                teamSize = projectFeatures.Team_Size,
                averageTeamSkillLevel = projectFeatures.Avg_Team_Skill_Level,
                rawComplexityScore = projectFeatures.Raw_Complexity_Score,
                budgetUtilizationRate = projectFeatures.Budget_Utilization_Rate,
                overallRiskScore = projectFeatures.Overall_Risk_Score
            };

            return AIToolResult.Success(result);
        }
    }
}
