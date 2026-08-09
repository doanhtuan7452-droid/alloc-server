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

        Task<WorkspaceRoleSummaryResponse> CreateWorkspaceRoleAsync(int accountId, int workspaceId, CreateWorkspaceRoleRequest request);
        Task<bool> UpdateWorkspaceRoleAsync(int accountId, int workspaceId, int roleId, UpdateWorkspaceRoleRequest request);
        Task<bool> DeleteWorkspaceRoleAsync(int accountId, int workspaceId, int roleId);
        Task<bool> UpdateRolePermissionsAsync(int accountId, int workspaceId, int roleId, UpdateRolePermissionsRequest request);
        Task<WorkspaceRoleDetailResponse?> GetWorkspaceRoleDetailsAsync(int accountId, int workspaceId, int roleId);
        Task<List<WorkspacePermissionResponse>> GetAvailablePermissionsAsync(int accountId, int workspaceId);
        Task<bool> UpdateMemberRoleAsync(int accountId, int workspaceId, int targetMemberId, UpdateMemberRoleRequest request);
        Task<bool> UpdateMemberSalaryOTAsync(int accountId, int workspaceId, int targetMemberId, UpdateMemberSalaryOTRequest request);
        Task<MemberSalaryOTResponse?> GetMemberSalaryOTAsync(int accountId, int workspaceId, int targetMemberId);
        Task<object> SearchWorkspaceAsync(int workspaceId, int currentAccountId, GetWorkspaceSearchQuery query);
    }
}
