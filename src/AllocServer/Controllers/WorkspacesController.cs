using AllocServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AllocServer.DTOs.Workspaces;
using AllocServer.DTOs.Common;
using AllocServer.DTOs.Projects;
using AllocServer.Data;
using AllocServer.Exceptions;
using AllocServer.Interfaces.ProjectAssets;
using AllocServer.Interfaces.Workspaces;
using AllocServer.Filters;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class WorkspacesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWorkspaceService _workspaceService;
        private readonly IProjectAssetService _projectAssetService;

        public WorkspacesController(
            ApplicationDbContext context,
            IWorkspaceService workspaceService,
            IProjectAssetService projectAssetService)
        {
            _context = context;
            _workspaceService = workspaceService;
            _projectAssetService = projectAssetService;
        }

        /// <summary>Lay danh sach Workspace ma user hien tai dang tham gia.</summary>
        [HttpGet]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(List<WorkspaceListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyWorkspaces()
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            var workspaces = await _workspaceService.GetCurrentUserWorkspacesAsync(accountId);
            return Ok(workspaces);
        }

        /// <summary>Tao Workspace moi.</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CreateWorkspaceResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. Lấy AccountId từ Token
            var accountIdStr = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(accountIdStr) || !int.TryParse(accountIdStr, out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            // Lấy ResourceID tương ứng
            var resource = await _context.Resources.FirstOrDefaultAsync(r => r.AccountID == accountId);
            if (resource == null)
            {
                return BadRequest(new ApiResponse { Message = "Tài khoản chưa có profile (Resource)." });
            }

            // Bắt đầu Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. Tạo Workspace
                var workspace = new Workspace
                {
                    Name = request.Name,
                    Type = request.Type
                };
                _context.Workspaces.Add(workspace);
                await _context.SaveChangesAsync(); // Cần save để lấy WorkspaceID

                // 3. Tạo Role đầu tiên (Owner)
                var ownerRole = new WorkspaceRole
                {
                    WorkspaceID = workspace.WorkspaceID,
                    RoleName = "Owner",
                    IsTemplate = false
                };
                _context.WorkspaceRoles.Add(ownerRole);
                await _context.SaveChangesAsync(); // Cần save để lấy WorkspaceRoleID

                // 4. Thêm người tạo vào WorkspaceMembers
                var workspaceMember = new WorkspaceMember
                {
                    WorkspaceID = workspace.WorkspaceID,
                    ResourceID = resource.ResourceID,
                    EmployeeCode = $"EMP{resource.ResourceID:D4}", // Demo logic cấp mã NV
                    WorkspaceRoleID = ownerRole.WorkspaceRoleID,
                    Status = "Active"
                };
                _context.WorkspaceMembers.Add(workspaceMember);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Lưu ý: Gói FREE đã được tự động thêm vào nhờ Database Trigger trg_AutoAssignFreePlan

                return StatusCode(201, new CreateWorkspaceResponse { Message = "Tạo Workspace thành công", WorkspaceId = workspace.WorkspaceID });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse { Message = $"Lỗi khi tạo Workspace: {ex.Message}" });
            }
        }

        /// <summary>Lay chi tiet Workspace.</summary>
        [HttpGet("{workspaceId}")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize] // Custom filter để chống IDOR
        [ProducesResponseType(typeof(WorkspaceDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkspaceDetails(int workspaceId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            var workspace = await _workspaceService.GetWorkspaceDetailsAsync(accountId, workspaceId);
            if (workspace == null)
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Workspace." });
            }

            return Ok(workspace);
        }

        /// <summary>Cap nhat thong tin Workspace.</summary>
        [HttpPut("{workspaceId}")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateWorkspace(
            int workspaceId,
            [FromBody] UpdateWorkspaceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var result = await _workspaceService.UpdateWorkspaceAsync(accountId, workspaceId, request);
                if (!result)
                {
                    return NotFound(new ApiResponse { Message = "Khong tim thay Workspace hoac ban khong co quyen truy cap." });
                }

                return Ok(new ApiResponse { Message = "Cap nhat Workspace thanh cong." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Xoa mem Workspace.</summary>
        [HttpDelete("{workspaceId}")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteWorkspace(int workspaceId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var result = await _workspaceService.DeleteWorkspaceAsync(accountId, workspaceId);
                if (!result)
                {
                    return NotFound(new ApiResponse { Message = "Khong tim thay Workspace hoac ban khong co quyen truy cap." });
                }

                return Ok(new ApiResponse { Message = "Xoa Workspace thanh cong." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Moi nhan su moi vao Workspace.</summary>
        [HttpPost("{workspaceId}/members")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(WorkspaceMemberDetailResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> InviteWorkspaceMember(
            int workspaceId,
            [FromBody] InviteWorkspaceMemberRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var member = await _workspaceService.InviteMemberAsync(accountId, workspaceId, request);
                if (member == null)
                {
                    return NotFound(new ApiResponse { Message = "Khong tim thay Workspace hoac ban khong co quyen truy cap." });
                }

                return StatusCode(201, member);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
            catch (QuotaExceededException ex)
            {
                return StatusCode(409, new ApiResponse { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
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

        /// <summary>Vo hieu hoa hoac kich hoat lai nhan su trong Workspace.</summary>
        [HttpPut("{workspaceId}/members/{memberId}/status")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateWorkspaceMemberStatus(
            int workspaceId,
            int memberId,
            [FromBody] UpdateMemberStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var result = await _workspaceService.UpdateMemberStatusAsync(
                    accountId,
                    workspaceId,
                    memberId,
                    request);

                if (!result)
                {
                    return NotFound(new ApiResponse { Message = "Khong tim thay nhan su trong Workspace." });
                }

                return Ok(new ApiResponse { Message = "Cap nhat trang thai nhan su thanh cong." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
            catch (QuotaExceededException ex)
            {
                return StatusCode(409, new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lay danh sach thanh vien cua Workspace theo phan trang.</summary>
        [HttpGet("{workspaceId}/Members")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(PagedWorkspaceMembersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetWorkspaceMembers(
            int workspaceId,
            [FromQuery] GetWorkspaceMembersQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var members = await _workspaceService.GetWorkspaceMembersAsync(workspaceId, query);
            return Ok(members);
        }

        /// <summary>Lay danh sach du an cua Workspace.</summary>
        [HttpGet("{workspaceId}/Projects")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(List<WorkspaceProjectListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetWorkspaceProjects(
            int workspaceId,
            [FromQuery] GetWorkspaceProjectsQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var projects = await _workspaceService.GetWorkspaceProjectsAsync(workspaceId, query);
            return Ok(projects);
        }

        /// <summary>Upload tai lieu chung cua Workspace de dung cho Direct/Group chat.</summary>
        [HttpPost("{workspaceId}/assets")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ProjectAssetResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UploadWorkspaceAsset(
            int workspaceId,
            [FromForm] UploadAssetRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var asset = await _projectAssetService.UploadWorkspaceAssetAsync(
                    accountId,
                    workspaceId,
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

        /// <summary>Tao du an moi trong Workspace.</summary>
        [HttpPost("{workspaceId}/Projects")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateProject(
            int workspaceId,
            [FromBody] CreateProjectRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            try
            {
                var project = await _workspaceService.CreateProjectAsync(accountId, workspaceId, request);
                return StatusCode(201, project);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
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

        /// <summary>Lay danh sach vai tro co san trong Workspace.</summary>
        [HttpGet("{workspaceId}/Roles")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(List<WorkspaceRoleSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWorkspaceRoles(int workspaceId)
        {
            var roles = await _workspaceService.GetWorkspaceRolesAsync(workspaceId);
            return Ok(roles);
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
