using System.Collections.Generic;
using System.Threading.Tasks;
using AllocServer.Models.AI;

namespace AllocServer.Interfaces.AI
{
    public interface IAIToolDispatcher
    {
        Task<AIToolResult> DispatchAsync(string toolName, Dictionary<string, object> arguments, string? idempotencyKey);
    }
}
