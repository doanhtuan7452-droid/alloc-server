namespace AllocServer.Interfaces
{
    public interface IFeatureQuotaService
    {
        Task<bool> CheckFeatureQuotaAsync(int workspaceId, string featureCode, int currentCount = 0);
    }
}
