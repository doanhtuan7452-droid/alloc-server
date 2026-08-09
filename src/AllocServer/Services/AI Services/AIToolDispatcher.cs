using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AllocServer.Data;
using AllocServer.Interfaces.AI;
using AllocServer.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.AI_Services
{
    public class AIToolDispatcher : IAIToolDispatcher
    {
        private readonly IEnumerable<IAIToolHandler> _handlers;
        private readonly IAIToolSafetyGuard _safetyGuard;
        private readonly ApplicationDbContext _dbContext;

        public AIToolDispatcher(
            IEnumerable<IAIToolHandler> handlers,
            IAIToolSafetyGuard safetyGuard,
            ApplicationDbContext dbContext)
        {
            _handlers = handlers;
            _safetyGuard = safetyGuard;
            _dbContext = dbContext;
        }

        public async Task<AIToolResult> DispatchAsync(string toolName, Dictionary<string, object> arguments, string? idempotencyKey)
        {
            var stopwatch = Stopwatch.StartNew();
            AIToolResult executionResult = AIToolResult.Failure(500, "INITIAL_ERROR", "Khởi tạo Dispatch thất bại.");
            string? errorCode = null;
            string? errorMessage = null;

            try
            {
                // 1. Chạy tầng Safety Guard để kiểm tra phân quyền, idempotency, loop, và cross-field check
                var guardResult = await _safetyGuard.ValidateExecutionAsync(toolName, arguments, idempotencyKey);
                
                if (!guardResult.IsPassed)
                {
                    errorCode = guardResult.ErrorCode ?? "VALIDATION_FAILED";
                    errorMessage = guardResult.ErrorMessage ?? "Kiểm tra an toàn thất bại.";
                    executionResult = AIToolResult.Failure(
                        guardResult.StatusCode, 
                        errorCode, 
                        errorMessage
                    );
                }
                // Nếu trúng Idempotency (gọi lại thành công trước đó), trả ngay kết quả cũ
                else if (guardResult.CachedResult is AIToolResult cachedResult)
                {
                    executionResult = cachedResult;
                }
                else
                {
                    // 2. Tìm Handler phù hợp
                    var handler = _handlers.FirstOrDefault(h => string.Equals(h.ToolName, toolName, StringComparison.OrdinalIgnoreCase));
                    if (handler == null)
                    {
                        errorCode = "HANDLER_NOT_FOUND";
                        errorMessage = $"Không tìm thấy handler xử lý cho công cụ '{toolName}'.";
                        executionResult = AIToolResult.Failure(404, errorCode, errorMessage);
                    }
                    else
                    {
                        // 3. Thực thi Handler
                        try
                        {
                            executionResult = await handler.ExecuteAsync(arguments);
                            if (!executionResult.IsSuccess)
                            {
                                errorCode = executionResult.ErrorCode;
                                errorMessage = executionResult.ErrorMessage;
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCode = "EXECUTION_ERROR";
                            errorMessage = $"Lỗi hệ thống khi thực thi công cụ: {ex.Message}";
                            executionResult = AIToolResult.Failure(500, errorCode, errorMessage);
                        }
                    }
                }

                // 4. Lưu cache Idempotency nếu thực thi thành công
                if (executionResult.IsSuccess && !string.IsNullOrEmpty(idempotencyKey))
                {
                    await _safetyGuard.CacheSuccessfulResponseAsync(idempotencyKey, executionResult);
                }
            }
            catch (Exception ex)
            {
                errorCode = "DISPATCH_ERROR";
                errorMessage = ex.Message;
                executionResult = AIToolResult.Failure(500, errorCode, errorMessage);
            }
            finally
            {
                stopwatch.Stop();

                // Lưu nhật ký gọi Webhook AI Tool
                try
                {
                    int? logAccountId = null;
                    if (arguments.TryGetValue("userId", out var uIdObj) && int.TryParse(uIdObj?.ToString(), out var uId))
                    {
                        logAccountId = uId;
                    }

                    int? logWorkspaceId = null;
                    if (arguments.TryGetValue("workspaceId", out var wIdObj) && int.TryParse(wIdObj?.ToString(), out var wId))
                    {
                        logWorkspaceId = wId;
                    }

                    // Nếu không có workspaceId trực tiếp mà có projectId, truy vấn để tìm WorkspaceID tương ứng
                    if ((!logWorkspaceId.HasValue || logWorkspaceId <= 0) && arguments.TryGetValue("projectId", out var pIdObj) && int.TryParse(pIdObj?.ToString(), out var pId))
                    {
                        var project = await _dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectID == pId && !p.IsDeleted);
                        if (project != null)
                        {
                            logWorkspaceId = project.WorkspaceID;
                        }
                    }

                    // Loại bỏ contextSignature nhạy cảm trước khi lưu đối số vào database log
                    var cleanArguments = new Dictionary<string, object>(arguments);
                    cleanArguments.Remove("contextSignature");

                    var log = new AIToolExecutionLog
                    {
                        WorkspaceID = logWorkspaceId > 0 ? logWorkspaceId : null,
                        AccountID = logAccountId > 0 ? logAccountId : null,
                        ToolName = toolName,
                        Arguments = JsonSerializer.Serialize(cleanArguments),
                        IsSuccess = executionResult.IsSuccess,
                        StatusCode = executionResult.StatusCode,
                        ErrorCode = errorCode,
                        ErrorMessage = errorMessage,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _dbContext.AIToolExecutionLogs.Add(log);
                    await _dbContext.SaveChangesAsync();
                }
                catch
                {
                    // Thất bại trong việc lưu log không ảnh hưởng đến luồng trả về kết quả
                }
            }

            return executionResult;
        }
    }
}
