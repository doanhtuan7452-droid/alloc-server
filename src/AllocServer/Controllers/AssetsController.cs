using AllocServer.DTOs.Common;
using AllocServer.DTOs.Projects;
using AllocServer.Filters;
using AllocServer.Interfaces.ProjectAssets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/assets")]
    public class AssetsController : ControllerBase
    {
        private readonly IProjectAssetService _projectAssetService;

        public AssetsController(IProjectAssetService projectAssetService)
        {
            _projectAssetService = projectAssetService;
        }

        /// <summary>Lay URL tai file tam thoi.</summary>
        [HttpGet("{assetId}/download")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(ProjectAssetDownloadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadAsset(int assetId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var response = await _projectAssetService.GetAssetDownloadUrlAsync(accountId, assetId);
                return Ok(response);
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

        /// <summary>Xoa mem tai lieu.</summary>
        [HttpDelete("{assetId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsset(int assetId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                await _projectAssetService.DeleteAssetAsync(accountId, assetId);
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
