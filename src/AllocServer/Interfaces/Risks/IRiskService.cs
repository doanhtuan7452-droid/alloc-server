using AllocServer.DTOs.Risks;
using AllocServer.Models;

namespace AllocServer.Interfaces.Risks
{
    public interface IRiskService
    {
        Task<PagedProjectRisksResponse> GetProjectRisksAsync(
            Project project,
            GetProjectRisksQuery query);

        Task<RiskDetailResponse> CreateProjectRiskAsync(
            int accountId,
            Project project,
            CreateRiskRequest request);

        Task<RiskMitigationResponse> CreateRiskMitigationAsync(
            int accountId,
            Risk risk,
            CreateRiskMitigationRequest request);

        Task<List<RiskLifecycleResponse>> GetRiskLifecycleAsync(Risk risk);

        Task<RiskDetailResponse> UpdateRiskAsync(
            int accountId,
            Risk risk,
            UpdateRiskRequest request);
    }
}
