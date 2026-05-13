using AllocServer.DTOs.Workspaces;
using AllocServer.Models;

namespace AllocServer.Interfaces.Projects
{
    public interface IProjectService
    {
        ProjectDetailResponse GetProject(Project project);

        Task<ProjectDetailResponse> UpdateProjectAsync(
            int accountId,
            Project project,
            UpdateProjectRequest request);

        Task DeleteProjectAsync(int accountId, Project project);
    }
}
