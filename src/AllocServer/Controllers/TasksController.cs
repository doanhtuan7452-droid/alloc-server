using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AllocServer.DTOs.Common;
using AllocServer.DTOs.Tasks;
using AllocServer.Interfaces.Tasks;
using AllocServer.Filters;
using AllocServer.Models;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>Cap nhat chi tiet task.</summary>
        [HttpPut("{taskId}")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.Update)]
        [ProducesResponseType(typeof(ProjectTaskDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTask(
            int taskId,
            [FromBody] UpdateProjectTaskRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var updatedTask = await _taskService.UpdateProjectTaskAsync(
                    task,
                    project,
                    request);

                return Ok(updatedTask);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Xoa mem task.</summary>
        [HttpDelete("{taskId}")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            try
            {
                await _taskService.DeleteProjectTaskAsync(accountId, task);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Gan member vao task theo vai tro.</summary>
        [HttpPost("{taskId}/assignees")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.Update)]
        [ProducesResponseType(typeof(TaskAssigneeResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignTaskAssignee(
            int taskId,
            [FromBody] AssignTaskAssigneeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            try
            {
                var response = await _taskService.AssignTaskAssigneeAsync(task, request);
                return StatusCode(201, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Go tat ca vai tro assign cua member khoi task.</summary>
        [HttpDelete("{taskId}/assignees/{memberId}")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.Update)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveTaskAssignee(
            int taskId,
            int memberId)
        {
            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            try
            {
                var removed = await _taskService.RemoveTaskAssigneeAsync(task, memberId);
                if (!removed)
                {
                    return NotFound(new ApiResponse { Message = "Khong tim thay assignment cua member trong task." });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Tao dependency cho task hien tai.</summary>
        [HttpPost("{taskId}/dependencies")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.Update)]
        [ProducesResponseType(typeof(TaskDependencyResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateTaskDependency(
            int taskId,
            [FromBody] CreateTaskDependencyRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            try
            {
                var response = await _taskService.CreateTaskDependencyAsync(task, request);
                return StatusCode(201, response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lay danh sach comment cua task.</summary>
        [HttpGet("{taskId}/comments")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.View)]
        [ProducesResponseType(typeof(List<TaskCommentResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTaskComments(int taskId)
        {
            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            var response = await _taskService.GetTaskCommentsAsync(task);
            return Ok(response);
        }

        /// <summary>Tao moi comment.</summary>
        [HttpPost("{taskId}/comments")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.Update)]
        [ProducesResponseType(typeof(TaskCommentResponse), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateTaskComment(
            int taskId,
            [FromBody] CreateTaskCommentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            try
            {
                var response = await _taskService.CreateTaskCommentAsync(accountId, task, request);
                return StatusCode(201, response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lay danh sach tai lieu cua task.</summary>
        [HttpGet("{taskId}/assets")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.View)]
        [ProducesResponseType(typeof(List<TaskAssetResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTaskAssets(int taskId)
        {
            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            var response = await _taskService.GetTaskAssetsAsync(task);
            return Ok(response);
        }

        /// <summary>Gan tai lieu vao task.</summary>
        [HttpPost("{taskId}/assets")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.Update)]
        [ProducesResponseType(typeof(List<TaskAssetResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> AttachTaskAssets(
            int taskId,
            [FromBody] AttachTaskAssetRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            try
            {
                var response = await _taskService.AttachTaskAssetsAsync(accountId, task, request);
                return StatusCode(201, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Go tai lieu khoi task.</summary>
        [HttpDelete("{taskId}/assets/{assetId}")]
        [Authorize]
        [RequireActiveAccount]
        [TaskAuthorize(TaskPermissionIds.Update)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DetachTaskAsset(
            int taskId,
            int assetId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentTask(out var task))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Task." });
            }

            try
            {
                await _taskService.DetachTaskAssetAsync(accountId, task, assetId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }

        private bool TryGetCurrentTask(out ProjectTask task)
        {
            if (HttpContext.Items.TryGetValue(TaskAuthorizeAttribute.CurrentTaskItemKey, out var item)
                && item is ProjectTask currentTask)
            {
                task = currentTask;
                return true;
            }

            task = null!;
            return false;
        }

        private bool TryGetCurrentProject(out Project project)
        {
            if (HttpContext.Items.TryGetValue(ProjectAuthorizeAttribute.CurrentProjectItemKey, out var item)
                && item is Project currentProject)
            {
                project = currentProject;
                return true;
            }

            project = null!;
            return false;
        }

        private bool TryGetCurrentAccountId(out int accountId)
        {
            if (HttpContext.Items.TryGetValue(RequireActiveAccountFilter.CurrentAccountIdItemKey, out var item)
                && item is int currentAccountId)
            {
                accountId = currentAccountId;
                return true;
            }

            var accountIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                 ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(accountIdClaim, out accountId);
        }
    }
}
