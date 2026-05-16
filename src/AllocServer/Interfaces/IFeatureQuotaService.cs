using AllocServer.DTOs.Common;

namespace AllocServer.Interfaces
{
    public interface IFeatureQuotaService
    {
        Task<bool> CheckFeatureQuotaAsync(int workspaceId, string featureCode, int currentCount = 0);

        Task<FeatureLimitInfo?> GetFeatureLimitAsync(int workspaceId, string featureCode);
    }
}
