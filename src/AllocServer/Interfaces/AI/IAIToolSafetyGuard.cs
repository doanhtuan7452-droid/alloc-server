using System.Collections.Generic;
using System.Threading.Tasks;
using AllocServer.Models.AI;

namespace AllocServer.Interfaces.AI
{
    public interface IAIToolSafetyGuard
    {
        Task<GuardValidationResult> ValidateExecutionAsync(string toolName, Dictionary<string, object> arguments, string? idempotencyKey);
        Task CacheSuccessfulResponseAsync(string idempotencyKey, AIToolResult result);
    }
}
