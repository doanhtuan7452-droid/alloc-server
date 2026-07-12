using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AllocServer.Constants.Permissions;
using AllocServer.Data;
using AllocServer.Interfaces.AI;
using AllocServer.Models.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AllocServer.Services.AI_Services
{
    public class AIToolSafetyGuard : IAIToolSafetyGuard
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIToolSafetyGuard> _logger;

        public AIToolSafetyGuard(
            ApplicationDbContext dbContext,
            IDistributedCache cache,
            IConfiguration configuration,
            ILogger<AIToolSafetyGuard> logger)
        {
            _dbContext = dbContext;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GuardValidationResult> ValidateExecutionAsync(string toolName, Dictionary<string, object> arguments, string? idempotencyKey)
        {
            // 1. Kiểm tra Idempotency (Chống trùng lặp do lỗi mạng)
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var cachedData = await _cache.GetStringAsync($"ai-tool-idempotency:{idempotencyKey}");
                if (!string.IsNullOrEmpty(cachedData))
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<AIToolResult>(cachedData);
                        if (result != null)
                        {
                            return GuardValidationResult.HitIdempotency(result);
                        }
                    }
                    catch
                    {
                        // Nếu cache hỏng thì cho chạy lại
                    }
                }
            }

            // 2. Kiểm tra Whitelist công cụ
            if (toolName != "create_project" && toolName != "create_task"
                && toolName != "get_project_info" && toolName != "get_employee_list" && toolName != "get_employee_detail"
                && toolName != "get_workspace_projects")
            {
                return GuardValidationResult.Fail("UNAUTHORIZED_TOOL", $"Công cụ '{toolName}' không được phép thực thi thông qua AI chat.", 403);
            }

            // 3. Phân tích tham số cơ bản và Throttling (Chống vòng lặp vô hạn của LLM)
            int userId = 0;
            int workspaceId = 0;
            int projectId = 0;

            if (arguments.TryGetValue("userId", out var uIdObj) && int.TryParse(uIdObj?.ToString(), out var uId))
            {
                userId = uId;
            }

            if (arguments.TryGetValue("workspaceId", out var wIdObj) && int.TryParse(wIdObj?.ToString(), out var wId))
            {
                workspaceId = wId;
            }

            if (arguments.TryGetValue("projectId", out var pIdObj) && int.TryParse(pIdObj?.ToString(), out var pId))
            {
                projectId = pId;
            }

            if (userId <= 0)
            {
                return GuardValidationResult.Fail("MISSING_USER_ID", "Tham số 'userId' không hợp lệ hoặc bị thiếu.");
            }

            // [FIST Pattern]: Kiểm tra projectId > 0 trước khi truy vấn CSDL để tìm workspaceId dự phòng
            if (workspaceId <= 0 && projectId > 0)
            {
                var project = await _dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectID == projectId && !p.IsDeleted);
                if (project != null)
                {
                    workspaceId = project.WorkspaceID;
                }
            }

            // Xác thực chữ ký số context chống User Impersonation / Prompt Injection
            var secretKey = _configuration["PythonServiceSettings:Internal:Secret"] ?? "default_secret";
            string receivedSignature = arguments.TryGetValue("contextSignature", out var sigObj) ? sigObj?.ToString() ?? string.Empty : string.Empty;
            string expectedSignature = GenerateContextSignature(userId, workspaceId, secretKey);

            if (string.IsNullOrEmpty(receivedSignature) || !string.Equals(receivedSignature, expectedSignature, StringComparison.Ordinal))
            {
                _logger.LogWarning("CẢNH BÁO BẢO MẬT: Phát hiện nỗ lực giả mạo ngữ cảnh cuộc gọi công cụ AI (FORGED_USER_CONTEXT)! " +
                                  "Công cụ: {ToolName}, UserId nhận được: {UserId}, WorkspaceId nhận được: {WorkspaceId}, " +
                                  "Chữ ký nhận được: '{RecvSig}', Chữ ký mong đợi: '{ExpectedSig}'", 
                                  toolName, userId, workspaceId, receivedSignature, expectedSignature);

                return GuardValidationResult.Fail("FORGED_USER_CONTEXT", "Chữ ký xác thực ngữ cảnh người dùng không hợp lệ hoặc bị thiếu. Yêu cầu bị từ chối.", 403);
            }

            // Tạo mã băm payload để kiểm tra throttling
            string payloadJson = JsonSerializer.Serialize(arguments);
            string payloadHash = ComputeMd5Hash(payloadJson);
            
            // Key throttle theo cấu trúc: ai-tool-throttle:{userId}:{workspaceId/projectId}:{toolName}:{hash}
            int scopeId = (toolName == "create_project" || toolName == "get_workspace_projects" || toolName == "get_employee_list" || toolName == "get_employee_detail") ? workspaceId : projectId;
            string throttleKey = $"ai-tool-throttle:{userId}:{scopeId}:{toolName}:{payloadHash}";

            var existingThrottle = await _cache.GetStringAsync(throttleKey);
            if (!string.IsNullOrEmpty(existingThrottle))
            {
                return GuardValidationResult.Fail("DUPLICATE_CALL_LOOP", "Bạn vừa thực hiện hành động này rồi, vui lòng đợi 30 giây hoặc sửa lại tham số để tránh lặp lại.", 400);
            }

            // Lưu dấu throttle 30 giây để ngăn LLM lặp lại
            var throttleOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            };
            await _cache.SetStringAsync(throttleKey, "active", throttleOptions);

            // 4. Xác thực chéo logic nghiệp vụ (Cross-field validation)
            if (toolName == "create_project")
            {
                if (workspaceId <= 0)
                {
                    return GuardValidationResult.Fail("MISSING_WORKSPACE_ID", "Tham số 'workspaceId' không hợp lệ hoặc bị thiếu.");
                }

                // Check chéo thời gian bắt đầu & kết thúc
                if (arguments.TryGetValue("startDate", out var sDateObj) && arguments.TryGetValue("endDate", out var eDateObj))
                {
                    if (DateOnly.TryParse(sDateObj?.ToString(), out var startDate) && DateOnly.TryParse(eDateObj?.ToString(), out var endDate))
                    {
                        if (endDate < startDate)
                        {
                            return GuardValidationResult.Fail("INVALID_DATES", "Ngày kết thúc dự án không được nhỏ hơn ngày bắt đầu.");
                        }
                    }
                }
            }
            else if (toolName == "create_task")
            {
                if (projectId <= 0)
                {
                    return GuardValidationResult.Fail("MISSING_PROJECT_ID", "Tham số 'projectId' không hợp lệ hoặc bị thiếu.");
                }

                // Check chéo thời gian bắt đầu & kết thúc của Task
                if (arguments.TryGetValue("startDate", out var sDateObj) && arguments.TryGetValue("endDate", out var eDateObj))
                {
                    if (DateOnly.TryParse(sDateObj?.ToString(), out var startDate) && DateOnly.TryParse(eDateObj?.ToString(), out var endDate))
                    {
                        if (endDate < startDate)
                        {
                            return GuardValidationResult.Fail("INVALID_DATES", "Ngày kết thúc task không được nhỏ hơn ngày bắt đầu.");
                        }
                    }
                }

                // Check chéo EstimatedValue với DurationType (Ví dụ 1 task > 1000 giờ hoặc > 100 ngày là phi thực tế)
                string durationType = string.Empty;
                decimal estimatedValue = 0;

                if (arguments.TryGetValue("durationType", out var dtObj))
                {
                    durationType = dtObj?.ToString() ?? string.Empty;
                }
                if (arguments.TryGetValue("estimatedValue", out var evObj) && decimal.TryParse(evObj?.ToString(), out var ev))
                {
                    estimatedValue = ev;
                }

                if (string.Equals(durationType, "Hour", StringComparison.OrdinalIgnoreCase) && estimatedValue > 1000m)
                {
                    return GuardValidationResult.Fail("UNREALISTIC_ESTIMATE", "Thời lượng thực hiện nhiệm vụ (EstimatedValue) quá lớn cho đơn vị đã chọn (Hour > 1000h). Vui lòng chia nhỏ nhiệm vụ hoặc điền con số thực tế.");
                }
                if (string.Equals(durationType, "Day", StringComparison.OrdinalIgnoreCase) && estimatedValue > 100m)
                {
                    return GuardValidationResult.Fail("UNREALISTIC_ESTIMATE", "Thời lượng thực hiện nhiệm vụ (EstimatedValue) quá lớn cho đơn vị đã chọn (Day > 100 ngày). Vui lòng chia nhỏ nhiệm vụ hoặc điền con số thực tế.");
                }
            }

            // 5. Xác thực quyền nghiệp vụ (Business Rule Authorization)
            if (toolName == "create_project")
            {
                // Chỉ Owner mới được quyền tạo Project trong Workspace
                var membership = await _dbContext.WorkspaceMembers
                    .Include(m => m.WorkspaceRole)
                    .FirstOrDefaultAsync(m =>
                        m.WorkspaceID == workspaceId
                        && m.Resource.AccountID == userId
                        && m.Status == "Active"
                        && !m.Workspace.IsDeleted
                        && !m.Resource.IsDeleted);

                if (membership == null || !string.Equals(membership.WorkspaceRole.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Chỉ Owner mới có quyền tạo dự án mới trong Workspace này.", 403);
                }
            }
            else if (toolName == "create_task")
            {
                // Lấy Project để biết WorkspaceID
                var project = await _dbContext.Projects
                    .FirstOrDefaultAsync(p => p.ProjectID == projectId && !p.IsDeleted);

                if (project == null)
                {
                    return GuardValidationResult.Fail("PROJECT_NOT_FOUND", "Không tìm thấy dự án được chỉ định hoặc đã bị xóa.", 404);
                }

                // User phải là thành viên active trong workspace của dự án và có quyền task:create hoặc là Owner
                var membership = await _dbContext.WorkspaceMembers
                    .Include(m => m.WorkspaceRole)
                    .FirstOrDefaultAsync(m =>
                        m.WorkspaceID == project.WorkspaceID
                        && m.Resource.AccountID == userId
                        && m.Status == "Active"
                        && !m.Resource.IsDeleted);

                if (membership == null)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không phải là thành viên hoạt động trong Workspace của dự án này.", 403);
                }

                var isOwner = string.Equals(membership.WorkspaceRole.RoleName, "Owner", StringComparison.OrdinalIgnoreCase);
                var hasTaskCreatePermission = await _dbContext.RolePermissions
                    .AnyAsync(rp =>
                        rp.WorkspaceRoleID == membership.WorkspaceRoleID
                        && rp.PermissionID == TaskPermissionIds.Create);

                if (!isOwner && !hasTaskCreatePermission)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không có quyền tạo task mới trong dự án này.", 403);
                }
            }
            else if (toolName == "get_project_info")
            {
                // Phải là thành viên hoạt động trong Workspace chứa dự án và có quyền ai:ask hoặc ai:view
                var project = await _dbContext.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProjectID == projectId && !p.IsDeleted);

                if (project == null)
                {
                    return GuardValidationResult.Fail("PROJECT_NOT_FOUND", "Không tìm thấy dự án được chỉ định hoặc đã bị xóa.", 404);
                }

                var membership = await _dbContext.WorkspaceMembers
                    .Include(m => m.WorkspaceRole)
                    .FirstOrDefaultAsync(m =>
                        m.WorkspaceID == project.WorkspaceID
                        && m.Resource.AccountID == userId
                        && m.Status == "Active"
                        && !m.Resource.IsDeleted);

                if (membership == null)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không phải là thành viên hoạt động trong Workspace chứa dự án này.", 403);
                }

                var isOwner = string.Equals(membership.WorkspaceRole.RoleName, "Owner", StringComparison.OrdinalIgnoreCase);
                var hasAiViewPermission = await _dbContext.RolePermissions
                    .AnyAsync(rp =>
                        rp.WorkspaceRoleID == membership.WorkspaceRoleID
                        && (rp.PermissionID == AIPermissionIds.Ask || rp.PermissionID == AIPermissionIds.View));

                if (!isOwner && !hasAiViewPermission)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không có quyền truy cập dữ liệu phân tích dự án trong Workspace này.", 403);
                }
            }
            else if (toolName == "get_employee_list")
            {
                // Phải là thành viên hoạt động trong Workspace và có quyền ai:ask hoặc ai:view
                if (workspaceId <= 0 && projectId > 0)
                {
                    var project = await _dbContext.Projects
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProjectID == projectId && !p.IsDeleted);
                    if (project != null)
                    {
                        workspaceId = project.WorkspaceID;
                    }
                }

                if (workspaceId <= 0)
                {
                    return GuardValidationResult.Fail("MISSING_WORKSPACE_CONTEXT", "Bắt buộc truyền thông tin workspaceId hoặc projectId.");
                }

                var membership = await _dbContext.WorkspaceMembers
                    .Include(m => m.WorkspaceRole)
                    .FirstOrDefaultAsync(m =>
                        m.WorkspaceID == workspaceId
                        && m.Resource.AccountID == userId
                        && m.Status == "Active"
                        && !m.Resource.IsDeleted);

                if (membership == null)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không phải là thành viên hoạt động trong Workspace này.", 403);
                }

                var isOwner = string.Equals(membership.WorkspaceRole.RoleName, "Owner", StringComparison.OrdinalIgnoreCase);
                var hasAiViewPermission = await _dbContext.RolePermissions
                    .AnyAsync(rp =>
                        rp.WorkspaceRoleID == membership.WorkspaceRoleID
                        && (rp.PermissionID == AIPermissionIds.Ask || rp.PermissionID == AIPermissionIds.View));

                if (!isOwner && !hasAiViewPermission)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không có quyền truy cập thông tin thành viên dự án trong Workspace này.", 403);
                }
            }
            else if (toolName == "get_employee_detail")
            {
                // Phải là thành viên hoạt động trong Workspace
                // VÀ bắt buộc có quyền xem hồ sơ nhân viên (member_profiles:view) hoặc là Owner
                if (workspaceId <= 0)
                {
                    return GuardValidationResult.Fail("MISSING_WORKSPACE_ID", "Tham số 'workspaceId' không hợp lệ hoặc bị thiếu.");
                }

                var membership = await _dbContext.WorkspaceMembers
                    .Include(m => m.WorkspaceRole)
                    .FirstOrDefaultAsync(m =>
                        m.WorkspaceID == workspaceId
                        && m.Resource.AccountID == userId
                        && m.Status == "Active"
                        && !m.Resource.IsDeleted);

                if (membership == null)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không phải là thành viên hoạt động trong Workspace này.", 403);
                }

                var isOwner = string.Equals(membership.WorkspaceRole.RoleName, "Owner", StringComparison.OrdinalIgnoreCase);
                var hasProfileViewPermission = await _dbContext.RolePermissions
                    .AnyAsync(rp =>
                        rp.WorkspaceRoleID == membership.WorkspaceRoleID
                        && rp.PermissionID == MemberProfilePermissionIds.View);

                if (!isOwner && !hasProfileViewPermission)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không có quyền xem thông tin chi tiết hồ sơ nhân viên nhạy cảm.", 403);
                }
            }
            else if (toolName == "get_workspace_projects")
            {
                if (workspaceId <= 0)
                {
                    return GuardValidationResult.Fail("MISSING_WORKSPACE_ID", "Tham số 'workspaceId' không hợp lệ hoặc bị thiếu.");
                }

                // Phải là thành viên hoạt động trong Workspace chứa các dự án và có quyền ai:ask hoặc ai:view
                var membership = await _dbContext.WorkspaceMembers
                    .Include(m => m.WorkspaceRole)
                    .FirstOrDefaultAsync(m =>
                        m.WorkspaceID == workspaceId
                        && m.Resource.AccountID == userId
                        && m.Status == "Active"
                        && !m.Resource.IsDeleted
                        && !m.WorkspaceRole.IsDeleted);

                if (membership == null)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không phải là thành viên hoạt động trong Workspace này.", 403);
                }

                var isOwner = string.Equals(membership.WorkspaceRole.RoleName, "Owner", StringComparison.OrdinalIgnoreCase)
                              || membership.WorkspaceRole.IsTemplate;

                var hasAiViewPermission = await _dbContext.RolePermissions
                    .AnyAsync(rp =>
                        rp.WorkspaceRoleID == membership.WorkspaceRoleID
                        && (rp.PermissionID == AIPermissionIds.Ask || rp.PermissionID == AIPermissionIds.View));

                if (!isOwner && !hasAiViewPermission)
                {
                    return GuardValidationResult.Fail("FORBIDDEN", "Bạn không có quyền truy cập thông tin các dự án trong Workspace này.", 403);
                }
            }

            return GuardValidationResult.Pass();
        }

        public async Task CacheSuccessfulResponseAsync(string idempotencyKey, AIToolResult result)
        {
            if (string.IsNullOrEmpty(idempotencyKey)) return;

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) // TTL 15 phút
            };

            var dataString = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync($"ai-tool-idempotency:{idempotencyKey}", dataString, cacheOptions);
        }

        private static string ComputeMd5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private string GenerateContextSignature(int userId, int workspaceId, string secretKey)
        {
            var payload = $"{userId}:{workspaceId}";
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(secretKey);
            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
            
            byte[] hash = System.Security.Cryptography.HMACSHA256.HashData(keyBytes, payloadBytes);
            return Convert.ToBase64String(hash);
        }
    }
}
