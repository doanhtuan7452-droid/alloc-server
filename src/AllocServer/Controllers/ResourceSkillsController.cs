using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using AllocServer.DTOs.Common;
using AllocServer.DTOs.ResourceSkills;
using AllocServer.Filters;
using AllocServer.Interfaces.ResourceSkills;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class ResourceSkillsController : ControllerBase
    {
        private readonly IResourceSkillService _resourceSkillService;

        public ResourceSkillsController(IResourceSkillService resourceSkillService)
        {
            _resourceSkillService = resourceSkillService;
        }

        /// <summary>Lấy danh sách kỹ năng kèm Level của một Resource cụ thể.</summary>
        [HttpGet("resources/{resourceId}/skills")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(List<ResourceSkillResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResourceSkills(int resourceId)
        {
            try
            {
                var skills = await _resourceSkillService.GetResourceSkillsAsync(resourceId);
                return Ok(skills);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lấy danh sách kỹ năng của chính tài khoản đang đăng nhập.</summary>
        [HttpGet("accounts/me/skills")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(List<ResourceSkillResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMySkills()
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            try
            {
                var skills = await _resourceSkillService.GetMySkillsAsync(accountId);
                return Ok(skills);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Gán một kỹ năng mới cho nhân sự kèm Level (1-5).</summary>
        [HttpPost("resources/{resourceId}/skills")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(ResourceSkillResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignSkill(int resourceId, [FromBody] AssignResourceSkillRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            var isSystemAccount = IsCallerSystemAccount();

            try
            {
                var result = await _resourceSkillService.AssignSkillAsync(resourceId, request, accountId, isSystemAccount);
                return StatusCode(201, result);
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

        /// <summary>Cập nhật Level (1-5) của một kỹ năng đã gán cho nhân sự.</summary>
        [HttpPut("resources/{resourceId}/skills/{skillId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(ResourceSkillResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSkillLevel(int resourceId, int skillId, [FromBody] UpdateResourceSkillLevelRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            var isSystemAccount = IsCallerSystemAccount();

            try
            {
                var result = await _resourceSkillService.UpdateSkillLevelAsync(resourceId, skillId, request, accountId, isSystemAccount);
                return Ok(result);
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

        /// <summary>Gỡ kỹ năng khỏi nhân sự.</summary>
        [HttpDelete("resources/{resourceId}/skills/{skillId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveSkill(int resourceId, int skillId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            var isSystemAccount = IsCallerSystemAccount();

            try
            {
                await _resourceSkillService.RemoveSkillAsync(resourceId, skillId, accountId, isSystemAccount);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Cập nhật/thay thế hàng loạt danh sách kỹ năng của nhân sự trong một Transaction.</summary>
        [HttpPut("resources/{resourceId}/skills/batch")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(List<ResourceSkillResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BatchUpsertSkills(int resourceId, [FromBody] BatchUpsertResourceSkillsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            var isSystemAccount = IsCallerSystemAccount();

            try
            {
                var result = await _resourceSkillService.BatchUpsertSkillsAsync(resourceId, request, accountId, isSystemAccount);
                return Ok(result);
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

        private bool IsCallerSystemAccount()
        {
            return User.FindFirst("IsSystemAccount")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
