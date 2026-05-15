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
    }
}
