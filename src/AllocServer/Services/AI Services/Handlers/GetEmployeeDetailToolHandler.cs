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
    public class GetEmployeeDetailToolHandler : IAIToolHandler
    {
        private readonly ApplicationDbContext _context;

        public GetEmployeeDetailToolHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public string ToolName => "get_employee_detail";

        public async Task<AIToolResult> ExecuteAsync(Dictionary<string, object> arguments)
        {
            GetEmployeeDetailToolPayload payload;
            try
            {
                var json = JsonSerializer.Serialize(arguments);
                payload = JsonSerializer.Deserialize<GetEmployeeDetailToolPayload>(json)
                          ?? throw new ArgumentException("Không thể parse arguments sang GetEmployeeDetailToolPayload.");
            }
            catch (Exception ex)
            {
                return AIToolResult.Failure(400, "INVALID_PAYLOAD", $"Payload không hợp lệ: {ex.Message}");
            }

            var memberQuery = _context.WorkspaceMembers
                .Include(m => m.Resource)
                .Include(m => m.WorkspaceRole)
                .Where(m => m.WorkspaceID == payload.WorkspaceId && m.Status == "Active" && !m.Resource.IsDeleted);

            if (payload.WorkspaceMemberId.HasValue && payload.WorkspaceMemberId.Value > 0)
            {
                memberQuery = memberQuery.Where(m => m.WorkspaceMemberID == payload.WorkspaceMemberId.Value);
            }
            else if (!string.IsNullOrEmpty(payload.EmployeeCode))
            {
                memberQuery = memberQuery.Where(m => m.EmployeeCode == payload.EmployeeCode);
            }
            else
            {
                return AIToolResult.Failure(400, "MISSING_MEMBER_IDENTIFIER", "Bắt buộc truyền workspaceMemberId hoặc employeeCode.");
            }

            var member = await memberQuery.FirstOrDefaultAsync();
            if (member == null)
            {
                return AIToolResult.Failure(404, "MEMBER_NOT_FOUND", "Không tìm thấy nhân sự đang hoạt động trong workspace này.");
            }

            // Lấy profile đánh giá HR (Experience, Skills scores, PerformanceRating...)
            var profile = await _context.WorkspaceMemberProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.WorkspaceMemberID == member.WorkspaceMemberID && !p.IsDeleted);

            // Lấy danh sách kỹ năng chuyên môn
            var skills = await _context.ResourceSkills
                .AsNoTracking()
                .Include(rs => rs.Skill)
                .Where(rs => rs.ResourceID == member.ResourceID && !rs.Skill.IsDeleted)
                .Select(rs => new
                {
                    skillName = rs.Skill.SkillName,
                    level = rs.Level
                })
                .ToListAsync();

            // Khối lượng công việc hiện tại (từ View vw_MemberCurrentWorkload)
            var workload = await _context.MemberCurrentWorkloads
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WorkspaceMemberID == member.WorkspaceMemberID);

            // Hiệu suất lịch sử (từ View vw_MemberHistoricalPerformance - PreviousTaskSuccessRate có dạng nullable)
            var performance = await _context.MemberHistoricalPerformances
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.WorkspaceMemberID == member.WorkspaceMemberID);

            var result = new
            {
                workspaceMemberId = member.WorkspaceMemberID,
                employeeCode = member.EmployeeCode,
                fullName = member.Resource.FullName,
                phoneNumber = member.Resource.PhoneNumber,
                avatarUrl = member.Resource.AvatarURL,
                timezone = member.Resource.Timezone,
                roleName = member.WorkspaceRole.RoleName,
                joinedAt = member.JoinedAt.ToString("yyyy-MM-dd"),
                
                // Hồ sơ năng lực HR (Đã được SafetyGuard xác thực quyền xem)
                experienceYears = profile?.ExperienceYears ?? 0,
                educationLevel = profile?.EducationLevel,
                technicalSkillScore = profile?.TechnicalSkillScore ?? 0,
                communicationScore = profile?.CommunicationScore ?? 0,
                leadershipScore = profile?.LeadershipScore ?? 0,
                problemSolvingScore = profile?.ProblemSolvingScore ?? 0,
                avgSoftSkillScore = profile?.AvgSoftSkillScore ?? 0,
                attendanceRate = profile?.AttendanceRate ?? 100,
                conflictRate = profile?.ConflictRate ?? 0,
                performanceRating = profile?.PerformanceRating ?? "Average",
                lastEvaluatedAt = profile?.LastEvaluatedAt.ToString("yyyy-MM-dd"),

                // Kỹ năng chi tiết
                skills = skills,

                // Khối lượng công việc
                currentWorkload = new
                {
                    activeTaskCount = workload?.ActiveTaskCount ?? 0,
                    currentWorkloadValue = workload?.CurrentWorkloadValue ?? 0
                },

                // Thống kê lịch sử
                historicalPerformance = new
                {
                    totalCompletedTasks = performance?.TotalCompletedTasks ?? 0,
                    previousTaskSuccessRate = performance?.PreviousTaskSuccessRate,
                    efficiencyRatio = performance?.EfficiencyRatio
                }
            };

            return AIToolResult.Success(result);
        }
    }
}
