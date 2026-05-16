using AllocServer.DTOs.AIInsights;
using AllocServer.Models;

namespace AllocServer.Interfaces.AIInsights
{
    public interface IAIInsightService
    {
        Task<PagedAIInsightsResponse> GetProjectAIInsightsAsync(
            Project project,
            GetProjectAIInsightsQuery query);
    }
}
