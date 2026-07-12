using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllocServer.Interfaces.AI;
using AllocServer.Models.AI;

namespace AllocServer.Services.AI_Services
{
    public class AIToolDispatcher : IAIToolDispatcher
    {
        private readonly IEnumerable<IAIToolHandler> _handlers;
        private readonly IAIToolSafetyGuard _safetyGuard;

        public AIToolDispatcher(IEnumerable<IAIToolHandler> handlers, IAIToolSafetyGuard safetyGuard)
        {
            _handlers = handlers;
            _safetyGuard = safetyGuard;
        }

        public async Task<AIToolResult> DispatchAsync(string toolName, Dictionary<string, object> arguments, string? idempotencyKey)
        {
            // 1. Chạy tầng Safety Guard để kiểm tra phân quyền, idempotency, loop, và cross-field check
            var guardResult = await _safetyGuard.ValidateExecutionAsync(toolName, arguments, idempotencyKey);
            
            if (!guardResult.IsPassed)
            {
                return AIToolResult.Failure(
                    guardResult.StatusCode, 
                    guardResult.ErrorCode ?? "VALIDATION_FAILED", 
                    guardResult.ErrorMessage ?? "Kiểm tra an toàn thất bại."
                );
            }

            // Nếu trúng Idempotency (gọi lại thành công trước đó), trả ngay kết quả cũ
            if (guardResult.CachedResult is AIToolResult cachedResult)
            {
                return cachedResult;
            }

            // 2. Tìm Handler phù hợp
            var handler = _handlers.FirstOrDefault(h => string.Equals(h.ToolName, toolName, StringComparison.OrdinalIgnoreCase));
            if (handler == null)
            {
                return AIToolResult.Failure(404, "HANDLER_NOT_FOUND", $"Không tìm thấy handler xử lý cho công cụ '{toolName}'.");
            }

            // 3. Thực thi Handler
            AIToolResult executionResult;
            try
            {
                executionResult = await handler.ExecuteAsync(arguments);
            }
            catch (Exception ex)
            {
                executionResult = AIToolResult.Failure(500, "EXECUTION_ERROR", $"Lỗi hệ thống khi thực thi công cụ: {ex.Message}");
            }

            // 4. Lưu cache Idempotency nếu thực thi thành công
            if (executionResult.IsSuccess && !string.IsNullOrEmpty(idempotencyKey))
            {
                await _safetyGuard.CacheSuccessfulResponseAsync(idempotencyKey, executionResult);
            }

            return executionResult;
        }
    }
}
