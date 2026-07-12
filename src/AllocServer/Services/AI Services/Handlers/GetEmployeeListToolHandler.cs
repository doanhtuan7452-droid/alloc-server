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
    public class GetEmployeeListToolHandler : IAIToolHandler
    {
        private readonly ApplicationDbContext _context;

        public GetEmployeeListToolHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public string ToolName => "get_employee_list";

        public async Task<AIToolResult> ExecuteAsync(Dictionary<string, object> arguments)
        {
            GetEmployeeListToolPayload payload;
            try
            {
                var json = JsonSerializer.Serialize(arguments);
                payload = JsonSerializer.Deserialize<GetEmployeeListToolPayload>(json)
                          ?? throw new ArgumentException("Không thể parse arguments sang GetEmployeeListToolPayload.");
            }
            catch (Exception ex)
            {
                return AIToolResult.Failure(400, "INVALID_PAYLOAD", $"Payload không hợp lệ: {ex.Message}");
            }

            int resolvedWorkspaceId = 0;
            if (payload.WorkspaceId.HasValue && payload.WorkspaceId.Value > 0)
            {
                resolvedWorkspaceId = payload.WorkspaceId.Value;
            }
            else if (payload.ProjectId.HasValue && payload.ProjectId.Value > 0)
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProjectID == payload.ProjectId.Value && !p.IsDeleted);
                if (project != null)
                {
                    resolvedWorkspaceId = project.WorkspaceID;
                }
            }

            if (resolvedWorkspaceId <= 0)
            {
                return AIToolResult.Failure(400, "MISSING_WORKSPACE_CONTEXT", "Bắt buộc phải truyền workspaceId hoặc projectId hợp lệ.");
            }

            var members = await _context.WorkspaceMembers
                .AsNoTracking()
                .Include(m => m.Resource)
                .Include(m => m.WorkspaceRole)
                .Where(m => m.WorkspaceID == resolvedWorkspaceId 
                            && m.Status == "Active" 
                            && !m.Resource.IsDeleted)
                .Select(m => new
                {
                    workspaceMemberId = m.WorkspaceMemberID,
                    employeeCode = m.EmployeeCode,
                    fullName = m.Resource.FullName,
                    roleName = m.WorkspaceRole.RoleName,
                    joinedAt = m.JoinedAt.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToListAsync();

            return AIToolResult.Success(new { workspaceId = resolvedWorkspaceId, employees = members });
        }
    }
}
