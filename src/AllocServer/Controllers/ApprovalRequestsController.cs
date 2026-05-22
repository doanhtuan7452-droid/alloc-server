using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AllocServer.DTOs.Common;
using AllocServer.DTOs.Requests;
using AllocServer.Filters;
using AllocServer.Interfaces.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class ApprovalRequestsController : ControllerBase
    {
        private readonly IRequestService _requestService;

        public ApprovalRequestsController(IRequestService requestService)
        {
            _requestService = requestService;
        }

        /// <summary>Tao don xin nghi trong workspace.</summary>
        [HttpPost("workspaces/{workspaceId}/leave-requests")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(LeaveRequestResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateLeaveRequest(
            int workspaceId,
            [FromBody] CreateLeaveRequestRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var response = await _requestService.CreateLeaveRequestAsync(
                    accountId,
                    workspaceId,
                    request);

                return StatusCode(201, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Tao don OT trong workspace.</summary>
        [HttpPost("workspaces/{workspaceId}/ot-requests")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(OTRequestResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateOTRequest(
            int workspaceId,
            [FromBody] CreateOTRequestRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var response = await _requestService.CreateOTRequestAsync(
                    accountId,
                    workspaceId,
                    request);

                return StatusCode(201, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Duyet hoac tu choi don Leave/OT.</summary>
        [HttpPut("requests/{requestType}/{requestId}/approval")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(RequestReviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ReviewRequest(
            string requestType,
            int requestId,
            [FromBody] ReviewRequestRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var response = await _requestService.ReviewRequestAsync(
                    accountId,
                    requestType,
                    requestId,
                    request);

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
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
