using AllocServer.DTOs.Workspaces;

namespace AllocServer.Interfaces.Workspaces
{
    public interface IWorkspaceService
    {
        Task<List<WorkspaceListItemResponse>> GetCurrentUserWorkspacesAsync(int accountId);

        Task<WorkspaceDetailResponse?> GetWorkspaceDetailsAsync(int accountId, int workspaceId);

        Task<PagedWorkspaceMembersResponse> GetWorkspaceMembersAsync(
            int workspaceId,
            GetWorkspaceMembersQuery query);

        Task<List<WorkspaceProjectListItemResponse>> GetWorkspaceProjectsAsync(
            int workspaceId,
            GetWorkspaceProjectsQuery query);

        Task<ProjectDetailResponse> CreateProjectAsync(
            int accountId,
            int workspaceId,
            CreateProjectRequest request);


        Task<List<WorkspaceRoleSummaryResponse>> GetWorkspaceRolesAsync(int workspaceId);

        Task<bool> UpdateWorkspaceAsync(int accountId, int workspaceId, UpdateWorkspaceRequest request);

        Task<bool> DeleteWorkspaceAsync(int accountId, int workspaceId);

        Task<WorkspaceMemberDetailResponse?> InviteMemberAsync(
            int accountId,
            int workspaceId,
            InviteWorkspaceMemberRequest request);

        Task<bool> UpdateMemberStatusAsync(
            int accountId,
            int workspaceId,
            int targetMemberId,
            UpdateMemberStatusRequest request);
    }
}
