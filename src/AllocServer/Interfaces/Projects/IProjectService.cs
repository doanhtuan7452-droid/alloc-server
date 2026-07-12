using AllocServer.DTOs.Workspaces;
using AllocServer.DTOs.Projects;
using AllocServer.Models;

namespace AllocServer.Interfaces.Projects
{
    public interface IProjectService
    {
        Task<ProjectDetailResponse> GetProjectAsync(int projectId);

        Task<ProjectDetailResponse> UpdateProjectAsync(
            int accountId,
            int projectId,
            UpdateProjectRequest request);

        Task DeleteProjectAsync(int accountId, int projectId);

        Task<ProjectProgressResponse> GetProjectProgressAsync(int projectId);
    }
}
