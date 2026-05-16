using AllocServer.DTOs.AIInsights;

namespace AllocServer.Interfaces.AI
{
    public interface IAIAnalysisService
    {
        Task<AIAskResponse> AnalyzeAsync(int accountId, AIAskRequest request);
    }
}
