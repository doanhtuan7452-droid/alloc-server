using System.Collections.Generic;
using System.Threading.Tasks;
using AllocServer.Models.AI;

namespace AllocServer.Interfaces.AI
{
    public interface IAIToolHandler
    {
        string ToolName { get; }
        Task<AIToolResult> ExecuteAsync(Dictionary<string, object> arguments);
    }
}
