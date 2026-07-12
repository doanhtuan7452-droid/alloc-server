using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AllocServer.Data;
using AllocServer.DTOs.AIChat;
using AllocServer.DTOs.Tasks;
using AllocServer.Interfaces.AI;
using AllocServer.Interfaces.Tasks;
using AllocServer.Models.AI;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.AI_Services.Handlers
{
    public class CreateTaskToolHandler : IAIToolHandler
    {
        private readonly ITaskService _taskService;
        private readonly ApplicationDbContext _context;

        public CreateTaskToolHandler(ITaskService taskService, ApplicationDbContext context)
        {
            _taskService = taskService;
            _context = context;
        }

        public string ToolName => "create_task";

        public async Task<AIToolResult> ExecuteAsync(Dictionary<string, object> arguments)
        {
            CreateTaskToolPayload payload;
            try
            {
                var json = JsonSerializer.Serialize(arguments);
                payload = JsonSerializer.Deserialize<CreateTaskToolPayload>(json) 
                          ?? throw new ArgumentException("Không thể parse arguments sang CreateTaskToolPayload.");
            }
            catch (Exception ex)
            {
                return AIToolResult.Failure(400, "INVALID_PAYLOAD", $"Payload không hợp lệ: {ex.Message}");
            }

            // Tìm Project
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectID == payload.ProjectId && !p.IsDeleted);

            if (project == null)
            {
                return AIToolResult.Failure(404, "PROJECT_NOT_FOUND", $"Không tìm thấy dự án có ID {payload.ProjectId} hoặc dự án đã bị xóa.");
            }

            var request = new CreateProjectTaskRequest
            {
                TaskName = payload.TaskName,
                EstimatedValue = payload.EstimatedValue,
                DurationType = payload.DurationType,
                StartDate = payload.StartDate,
                EndDate = payload.EndDate,
                Status = payload.Status,
                Complexity = payload.Complexity,
                RequiredSkillLevel = payload.RequiredSkillLevel,
                Priority = payload.Priority,
                ExpectedTeamSize = payload.ExpectedTeamSize
            };

            try
            {
                var result = await _taskService.CreateProjectTaskAsync(payload.UserId, project, request);
                return AIToolResult.Success(result);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                return AIToolResult.Failure(400, "DUPLICATE_NAME", 
                    $"Tên nhiệm vụ '{payload.TaskName}' đã được sử dụng bởi một nhiệm vụ khác đang hoạt động trong dự án này. Vui lòng chọn một tên khác cụ thể hơn.");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("AlreadyExists") || ex.Message.Contains("Unique") || ex.Message.Contains("Duplicate"))
            {
                return AIToolResult.Failure(400, "DUPLICATE_NAME", 
                    $"Tên nhiệm vụ '{payload.TaskName}' đã được sử dụng bởi một nhiệm vụ khác đang hoạt động trong dự án này. Vui lòng chọn một tên khác cụ thể hơn.");
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
                return AIToolResult.Failure(500, "INTERNAL_ERROR", $"Lỗi hệ thống khi tạo task: {ex.Message}");
            }
        }
    }
}
