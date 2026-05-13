using AllocServer.DTOs.Tasks;
using AllocServer.Models;

namespace AllocServer.Interfaces.Tasks
{
    public interface ITaskService
    {
        Task<PagedProjectTasksResponse> GetProjectTasksAsync(
            Project project,
            GetProjectTasksQuery query);

        Task<ProjectTaskDetailResponse> CreateProjectTaskAsync(
            int accountId,
            Project project,
            CreateProjectTaskRequest request);

        Task<ProjectTaskDetailResponse> UpdateProjectTaskAsync(
            ProjectTask task,
            Project project,
            UpdateProjectTaskRequest request);

        Task<TaskAssigneeResponse> AssignTaskAssigneeAsync(
            ProjectTask task,
            AssignTaskAssigneeRequest request);

        Task<bool> RemoveTaskAssigneeAsync(
            ProjectTask task,
            int workspaceMemberId);

        Task<TaskDependencyResponse> CreateTaskDependencyAsync(
            ProjectTask successorTask,
            CreateTaskDependencyRequest request);

        Task DeleteProjectTaskAsync(int accountId, ProjectTask task);
    }
}
