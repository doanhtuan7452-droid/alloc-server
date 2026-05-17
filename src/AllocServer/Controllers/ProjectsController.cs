using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AllocServer.DTOs.Workspaces;
using AllocServer.DTOs.AIInsights;
using AllocServer.DTOs.Common;
using AllocServer.DTOs.Expenses;
using AllocServer.DTOs.Projects;
using AllocServer.DTOs.Revenues;
using AllocServer.DTOs.Risks;
using AllocServer.DTOs.Tasks;
using AllocServer.Interfaces.Expenses;
using AllocServer.Interfaces.Tasks;
using AllocServer.Interfaces.Risks;
using AllocServer.Interfaces.AIInsights;
using AllocServer.Filters;
using AllocServer.Interfaces.ProjectAssets;
using AllocServer.Interfaces.Projects;
using AllocServer.Interfaces.Revenues;
using AllocServer.Models;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/projects")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ITaskService _taskService;
        private readonly IExpenseService _expenseService;
        private readonly IRevenueService _revenueService;
        private readonly IRiskService _riskService;
        private readonly IAIInsightService _aiInsightService;
        private readonly IProjectAssetService _projectAssetService;

        public ProjectsController(
            IProjectService projectService,
            ITaskService taskService,
            IExpenseService expenseService,
            IRevenueService revenueService,
            IRiskService riskService,
            IAIInsightService aiInsightService,
            IProjectAssetService projectAssetService)
        {
            _projectService = projectService;
            _taskService = taskService;
            _expenseService = expenseService;
            _revenueService = revenueService;
            _riskService = riskService;
            _aiInsightService = aiInsightService;
            _projectAssetService = projectAssetService;
        }

        /// <summary>Lay chi tiet du an.</summary>
        [HttpGet("{projectId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize]
        [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public IActionResult GetProject(int projectId)
        {
            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            return Ok(_projectService.GetProject(project));
        }

        /// <summary>Cap nhat du an.</summary>
        [HttpPut("{projectId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(ProjectPermissionIds.Update)]
        [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateProject(
            int projectId,
            [FromBody] UpdateProjectRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var updatedProject = await _projectService.UpdateProjectAsync(
                    accountId,
                    project,
                    request);

                return Ok(updatedProject);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Xoa mem du an.</summary>
        [HttpDelete("{projectId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(ProjectPermissionIds.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            await _projectService.DeleteProjectAsync(accountId, project);
            return NoContent();
        }

        /// <summary>Lay danh sach task cua du an.</summary>
        [HttpGet("{projectId}/tasks")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize]
        [ProducesResponseType(typeof(PagedProjectTasksResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectTasks(
            int projectId,
            [FromQuery] GetProjectTasksQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var tasks = await _taskService.GetProjectTasksAsync(project, query);
                return Ok(tasks);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Tao task moi trong du an.</summary>
        [HttpPost("{projectId}/tasks")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(TaskPermissionIds.Create)]
        [ProducesResponseType(typeof(ProjectTaskDetailResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateProjectTask(
            int projectId,
            [FromBody] CreateProjectTaskRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var task = await _taskService.CreateProjectTaskAsync(
                    accountId,
                    project,
                    request);

                return StatusCode(201, task);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lay danh sach tai lieu cua du an.</summary>
        [HttpGet("{projectId}/assets")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(AssetPermissionIds.View)]
        [ProducesResponseType(typeof(PagedProjectAssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectAssets(
            int projectId,
            [FromQuery] GetProjectAssetsQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var assets = await _projectAssetService.GetProjectAssetsAsync(project, query);
                return Ok(assets);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Upload tai lieu moi vao du an.</summary>
        [HttpPost("{projectId}/assets")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(AssetPermissionIds.Create)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ProjectAssetResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadProjectAsset(
            int projectId,
            [FromForm] UploadAssetRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var asset = await _projectAssetService.UploadProjectAssetAsync(
                    accountId,
                    project,
                    request);

                return StatusCode(201, asset);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lay danh sach chi phi du an.</summary>
        [HttpGet("{projectId}/expenses")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(ExpensePermissionIds.View)]
        [ProducesResponseType(typeof(PagedProjectExpensesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectExpenses(
            int projectId,
            [FromQuery] GetProjectExpensesQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var expenses = await _expenseService.GetProjectExpensesAsync(project, query);
                return Ok(expenses);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Ghi nhan chi phi moi.</summary>
        [HttpPost("{projectId}/expenses")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(ExpensePermissionIds.Create)]
        [ProducesResponseType(typeof(ExpenseDetailResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateProjectExpense(
            int projectId,
            [FromBody] CreateExpenseRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var expense = await _expenseService.CreateProjectExpenseAsync(
                    accountId,
                    project,
                    request);

                return StatusCode(201, expense);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lay danh sach doanh thu du an.</summary>
        [HttpGet("{projectId}/revenues")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(RevenuePermissionIds.View)]
        [ProducesResponseType(typeof(PagedProjectRevenuesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectRevenues(
            int projectId,
            [FromQuery] GetProjectRevenuesQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var revenues = await _revenueService.GetProjectRevenuesAsync(project, query);
                return Ok(revenues);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lay danh sach rui ro du an.</summary>
        [HttpGet("{projectId}/risks")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(RiskPermissionIds.View)]
        [ProducesResponseType(typeof(PagedProjectRisksResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectRisks(
            int projectId,
            [FromQuery] GetProjectRisksQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var risks = await _riskService.GetProjectRisksAsync(project, query);
                return Ok(risks);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lay danh sach canh bao AI cua du an.</summary>
        [HttpGet("{projectId}/ai-insights")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(AIPermissionIds.View)]
        [ProducesResponseType(typeof(PagedAIInsightsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectAIInsights(
            int projectId,
            [FromQuery] GetProjectAIInsightsQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var insights = await _aiInsightService.GetProjectAIInsightsAsync(project, query);
                return Ok(insights);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Khai bao rui ro moi.</summary>
        [HttpPost("{projectId}/risks")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(RiskPermissionIds.Create)]
        [ProducesResponseType(typeof(RiskDetailResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateProjectRisk(
            int projectId,
            [FromBody] CreateRiskRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentProject(out var project))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Project." });
            }

            try
            {
                var risk = await _riskService.CreateProjectRiskAsync(
                    accountId,
                    project,
                    request);

                return StatusCode(201, risk);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
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
