using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AllocServer.DTOs.AIChat;
using AllocServer.DTOs.Workspaces;
using AllocServer.Interfaces.AI;
using AllocServer.Interfaces.Workspaces;
using AllocServer.Models.AI;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.AI_Services.Handlers
{
    public class CreateProjectToolHandler : IAIToolHandler
    {
        private readonly IWorkspaceService _workspaceService;

        public CreateProjectToolHandler(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        public string ToolName => "create_project";

        public async Task<AIToolResult> ExecuteAsync(Dictionary<string, object> arguments)
        {
            CreateProjectToolPayload payload;
            try
            {
                var json = JsonSerializer.Serialize(arguments);
                payload = JsonSerializer.Deserialize<CreateProjectToolPayload>(json) 
                          ?? throw new ArgumentException("Không thể parse arguments sang CreateProjectToolPayload.");
            }
            catch (Exception ex)
            {
                return AIToolResult.Failure(400, "INVALID_PAYLOAD", $"Payload không hợp lệ: {ex.Message}");
            }

            var request = new CreateProjectRequest
            {
                ProjectName = payload.ProjectName,
                ExpectedBudget = payload.ExpectedBudget,
                StartDate = payload.StartDate,
                EndDate = payload.EndDate,
                OriginalCurrencyCode = payload.OriginalCurrencyCode,
                ExchangeRateToUSD = payload.ExchangeRateToUSD,
                Methodology = payload.Methodology
            };

            try
            {
                var result = await _workspaceService.CreateProjectAsync(payload.UserId, payload.WorkspaceId, request);
                return AIToolResult.Success(result);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                return AIToolResult.Failure(400, "DUPLICATE_NAME", 
                    $"Tên dự án '{payload.ProjectName}' đã được sử dụng bởi một dự án khác đang hoạt động trong Workspace này. Vui lòng chọn một tên khác cụ thể hơn.");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("AlreadyExists") || ex.Message.Contains("Unique") || ex.Message.Contains("Duplicate"))
            {
                return AIToolResult.Failure(400, "DUPLICATE_NAME", 
                    $"Tên dự án '{payload.ProjectName}' đã được sử dụng bởi một dự án khác đang hoạt động trong Workspace này. Vui lòng chọn một tên khác cụ thể hơn.");
            }
            catch (ArgumentException ex)
            {
                return AIToolResult.Failure(400, "INVALID_ARGUMENT", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return AIToolResult.Failure(403, "FORBIDDEN", ex.Message);
            }
            catch (Exception ex)
            {
                return AIToolResult.Failure(500, "INTERNAL_ERROR", $"Lỗi hệ thống khi tạo dự án: {ex.Message}");
            }
        }
    }
}
