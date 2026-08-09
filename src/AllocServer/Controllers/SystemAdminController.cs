using AllocServer.DTOs.Common;
using AllocServer.DTOs.SystemAdmin;
using AllocServer.Filters;
using AllocServer.Interfaces.SystemAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize]
    [RequireActiveAccount]
    [RequireSystemAccount]
    public class SystemAdminController : ControllerBase
    {
        private readonly ISystemAdminService _adminService;

        public SystemAdminController(ISystemAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>Xem chi tiết tài khoản toàn hệ thống kèm danh sách Workspace đã tham gia.</summary>
        [HttpGet("accounts/{accountId}")]
        [ProducesResponseType(typeof(AccountAdminDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAccountDetails(int accountId)
        {
            var result = await _adminService.GetAccountDetailsForAdminAsync(accountId);
            if (result == null)
            {
                return NotFound(new ApiResponse { Message = "Không tìm thấy tài khoản." });
            }
            return Ok(result);
        }

        /// <summary>Khóa/Mở khóa tài khoản.</summary>
        [HttpPut("accounts/{accountId}/status")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAccountStatus(int accountId, [FromBody] UpdateAccountStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _adminService.UpdateAccountStatusAsync(accountId, request);
            if (!success)
            {
                return NotFound(new ApiResponse { Message = "Không tìm thấy tài khoản." });
            }

            return Ok(new ApiResponse { Message = $"Cập nhật trạng thái tài khoản thành '{request.Status}' thành công." });
        }

        /// <summary>Tắt/Bật quyền quản trị hệ thống.</summary>
        [HttpPut("accounts/{accountId}/system-role")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSystemRole(int accountId, [FromBody] UpdateSystemRoleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _adminService.UpdateSystemAccountRoleAsync(accountId, request);
            if (!success)
            {
                return NotFound(new ApiResponse { Message = "Không tìm thấy tài khoản." });
            }

            return Ok(new ApiResponse { Message = "Cập nhật quyền quản trị hệ thống thành công." });
        }

        /// <summary>Liệt kê danh sách tất cả Workspace.</summary>
        [HttpGet("workspaces")]
        [ProducesResponseType(typeof(PagedWorkspacesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWorkspaces([FromQuery] GetWorkspacesQuery query)
        {
            var result = await _adminService.GetWorkspacesForAdminAsync(query);
            return Ok(result);
        }

        /// <summary>Xem chi tiết một Workspace.</summary>
        [HttpGet("workspaces/{workspaceId}")]
        [ProducesResponseType(typeof(WorkspaceAdminDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkspaceDetails(int workspaceId)
        {
            var result = await _adminService.GetWorkspaceDetailsForAdminAsync(workspaceId);
            if (result == null)
            {
                return NotFound(new ApiResponse { Message = "Không tìm thấy Workspace." });
            }
            return Ok(result);
        }

        /// <summary>Cập nhật gói cước của Workspace.</summary>
        [HttpPut("workspaces/{workspaceId}/subscription")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateWorkspaceSubscription(int workspaceId, [FromBody] UpdateWorkspaceSubscriptionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _adminService.UpdateWorkspaceSubscriptionAsync(workspaceId, request);
            if (!success)
            {
                return BadRequest(new ApiResponse { Message = "Cập nhật gói cước thất bại. Vui lòng kiểm tra WorkspaceID hoặc PlanCode hợp lệ." });
            }

            return Ok(new ApiResponse { Message = "Cập nhật gói cước của Workspace thành công." });
        }

        /// <summary>Xem nhật ký gọi AI Tool.</summary>
        [HttpGet("ai/tool-logs")]
        [ProducesResponseType(typeof(PagedAIToolLogsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAIToolLogs([FromQuery] GetAIToolLogsQuery query)
        {
            var result = await _adminService.GetAIToolLogsAsync(query);
            return Ok(result);
        }

        /// <summary>Thống kê lượng sử dụng AI của tháng hiện tại.</summary>
        [HttpGet("ai/usages")]
        [ProducesResponseType(typeof(List<AIUsageAdminResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAIUsages()
        {
            var result = await _adminService.GetAIUsagesForAdminAsync();
            return Ok(result);
        }

        /// <summary>Giám sát Background Queue.</summary>
        [HttpGet("background-jobs/stats")]
        [ProducesResponseType(typeof(BackgroundJobStatsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBackgroundJobStats()
        {
            var result = await _adminService.GetBackgroundJobStatsAsync();
            return Ok(result);
        }
    }
}
