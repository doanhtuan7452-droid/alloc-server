using AllocServer.DTOs.AIInsights;
using AllocServer.DTOs.Common;
using AllocServer.Exceptions;
using AllocServer.Filters;
using AllocServer.Interfaces.AI;
using AllocServer.Interfaces.AIInsights;
using AllocServer.Constants.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/projects/{projectId}/ai-insights")]
    public class ProjectAIInsightsController : ControllerBase
    {
        private readonly IAIAnalysisService _aiAnalysisService;
        private readonly IAIInsightService _aiInsightService;

        public ProjectAIInsightsController(
            IAIAnalysisService aiAnalysisService,
            IAIInsightService aiInsightService)
        {
            _aiAnalysisService = aiAnalysisService;
            _aiInsightService = aiInsightService;
        }

        /// <summary>Gửi yêu cầu AI đánh giá mức độ rủi ro của dự án.</summary>
        [HttpPost("risk-assessment")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(AIPermissionIds.Ask)]
        [ProducesResponseType(typeof(AIAskResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssessProjectRisk(int projectId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var askRequest = new AIAskRequest
                {
                    ProjectId = projectId,
                    AnalysisType = "Risk Warning",
                    TargetEntityId = null,
                    Prompt = null
                };
                
                var response = await _aiAnalysisService.AnalyzeAsync(accountId, askRequest);
                return Ok(response);
            }
            catch (QuotaExceededException ex)
            {
                return StatusCode(403, new ApiResponse
                {
                    Message = ex.Message,
                    ErrorCode = ex.ErrorCode
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
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

        /// <summary>Gửi yêu cầu AI đánh giá độ phù hợp và rủi ro của nhân sự cho task trong dự án.</summary>
        [HttpPost("allocation-assessment")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(AIPermissionIds.Ask)]
        [ProducesResponseType(typeof(AIAskResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssessPersonnelAllocation(
            int projectId,
            [FromBody] AIAllocationAssessmentRequest request)
        {
            if (request == null || request.TaskId <= 0)
            {
                return BadRequest(new ApiResponse { Message = "taskId phai lon hon 0." });
            }

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var askRequest = new AIAskRequest
                {
                    ProjectId = projectId,
                    AnalysisType = "Resource Suggestion",
                    TargetEntityId = request.TaskId,
                    WorkspaceMemberIds = request.WorkspaceMemberIds
                };
                
                var response = await _aiAnalysisService.AnalyzeAsync(accountId, askRequest);
                return Ok(response);
            }
            catch (QuotaExceededException ex)
            {
                return StatusCode(403, new ApiResponse
                {
                    Message = ex.Message,
                    ErrorCode = ex.ErrorCode
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
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

        /// <summary>Cập nhật phản hồi người dùng và thẩm định nhãn rủi ro (Human-In-The-Loop).</summary>
        [HttpPatch("{logId}/feedback")]
        [Authorize]
        [RequireActiveAccount]
        [ProjectAuthorize(AIPermissionIds.Ask)]
        [ProducesResponseType(typeof(AIInsightResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateFeedback(
            int projectId,
            int logId,
            [FromBody] UpdateAILogFeedbackRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse { Message = "Request body khong hop le." });
            }

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var response = await _aiInsightService.UpdateLogFeedbackAsync(projectId, logId, accountId, request);
                return Ok(response);
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
