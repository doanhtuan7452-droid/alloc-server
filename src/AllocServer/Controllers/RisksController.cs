using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AllocServer.DTOs.Common;
using AllocServer.DTOs.Risks;
using AllocServer.Interfaces.Risks;
using AllocServer.Filters;
using AllocServer.Models;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/risks")]
    public class RisksController : ControllerBase
    {
        private readonly IRiskService _riskService;

        public RisksController(IRiskService riskService)
        {
            _riskService = riskService;
        }

        /// <summary>Tao ke hoach giam thieu rui ro.</summary>
        [HttpPost("{riskId}/mitigations")]
        [Authorize]
        [RequireActiveAccount]
        [RiskAuthorize(RiskPermissionIds.Create)]
        [ProducesResponseType(typeof(RiskMitigationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateRiskMitigation(
            int riskId,
            [FromBody] CreateRiskMitigationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            if (!TryGetCurrentRisk(out var risk))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Risk." });
            }

            try
            {
                var response = await _riskService.CreateRiskMitigationAsync(
                    accountId,
                    risk,
                    request);

                return StatusCode(201, response);
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

        /// <summary>Xem lich su thay doi trang thai rui ro.</summary>
        [HttpGet("{riskId}/lifecycle")]
        [Authorize]
        [RequireActiveAccount]
        [RiskAuthorize(RiskPermissionIds.View)]
        [ProducesResponseType(typeof(List<RiskLifecycleResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRiskLifecycle(int riskId)
        {
            if (!TryGetCurrentRisk(out var risk))
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay Risk." });
            }

            var response = await _riskService.GetRiskLifecycleAsync(risk);
            return Ok(response);
        }

        private bool TryGetCurrentRisk(out Risk risk)
        {
            if (HttpContext.Items.TryGetValue(RiskAuthorizeAttribute.CurrentRiskItemKey, out var item)
                && item is Risk currentRisk)
            {
                risk = currentRisk;
                return true;
            }

            risk = null!;
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
