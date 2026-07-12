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
            int accountId,
            ProjectTask task,
            AssignTaskAssigneeRequest request);

        Task<List<TaskAssigneeDetailResponse>> GetTaskAssigneesAsync(int taskId);

        Task<bool> RemoveTaskAssigneeAsync(
            ProjectTask task,
            int workspaceMemberId);

        Task<TaskDependencyResponse> CreateTaskDependencyAsync(
            ProjectTask successorTask,
            CreateTaskDependencyRequest request);

        Task DeleteProjectTaskAsync(int accountId, ProjectTask task);

        // Comments
        Task<List<TaskCommentResponse>> GetTaskCommentsAsync(ProjectTask task);
        
        Task<TaskCommentResponse> CreateTaskCommentAsync(
            int accountId, 
            ProjectTask task, 
            CreateTaskCommentRequest request);
            
        Task<TaskCommentResponse> UpdateTaskCommentAsync(
            int accountId, 
            int commentId, 
            UpdateTaskCommentRequest request);
            
        Task DeleteTaskCommentAsync(int accountId, int commentId);

        // Assets
        Task<List<TaskAssetResponse>> GetTaskAssetsAsync(ProjectTask task);
        
        Task<List<TaskAssetResponse>> AttachTaskAssetsAsync(
            int accountId, 
            ProjectTask task, 
            AttachTaskAssetRequest request);
            
        Task DetachTaskAssetAsync(
            int accountId, 
            ProjectTask task, 
            int assetId);
    }
}
