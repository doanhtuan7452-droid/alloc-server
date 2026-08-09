using System.Collections.Generic;
using System.Threading.Tasks;
using AllocServer.DTOs.SystemAdmin;

namespace AllocServer.Interfaces.SystemAdmin
{
    public interface ISystemAdminService
    {
        Task<AccountAdminDetailResponse?> GetAccountDetailsForAdminAsync(int accountId);
        Task<bool> UpdateAccountStatusAsync(int accountId, UpdateAccountStatusRequest request);
        Task<bool> UpdateSystemAccountRoleAsync(int accountId, UpdateSystemRoleRequest request);
        Task<PagedWorkspacesResponse> GetWorkspacesForAdminAsync(GetWorkspacesQuery query);
        Task<WorkspaceAdminDetailResponse?> GetWorkspaceDetailsForAdminAsync(int workspaceId);
        Task<bool> UpdateWorkspaceSubscriptionAsync(int workspaceId, UpdateWorkspaceSubscriptionRequest request);
        Task<PagedAIToolLogsResponse> GetAIToolLogsAsync(GetAIToolLogsQuery query);
        Task<List<AIUsageAdminResponse>> GetAIUsagesForAdminAsync();
        Task<BackgroundJobStatsResponse> GetBackgroundJobStatsAsync();
    }
}
