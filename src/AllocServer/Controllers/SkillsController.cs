using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using AllocServer.DTOs.Common;
using AllocServer.DTOs.Skills;
using AllocServer.Filters;
using AllocServer.Interfaces.Skills;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/skills")]
    public class SkillsController : ControllerBase
    {
        private readonly ISkillService _skillService;

        public SkillsController(ISkillService skillService)
        {
            _skillService = skillService;
        }

        /// <summary>Lấy danh sách kỹ năng trong hệ thống (Hỗ trợ phân trang, tìm kiếm hoặc lấy toàn bộ).</summary>
        [HttpGet]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(PagedSkillsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(List<SkillResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSkills([FromQuery] GetSkillsQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (query.All)
            {
                var allSkills = await _skillService.GetAllSkillsAsync(query.SearchTerm);
                return Ok(allSkills);
            }

            var pagedSkills = await _skillService.GetSkillsPagedAsync(query);
            return Ok(pagedSkills);
        }

        /// <summary>Lấy chi tiết một kỹ năng theo ID.</summary>
        [HttpGet("{skillId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(SkillResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSkillById(int skillId)
        {
            var skill = await _skillService.GetSkillByIdAsync(skillId);
            if (skill == null)
            {
                return NotFound(new ApiResponse { Message = "Không tìm thấy kỹ năng." });
            }

            return Ok(skill);
        }

        /// <summary>Thêm mới kỹ năng vào danh mục toàn cục (Chỉ System Admin).</summary>
        [HttpPost]
        [Authorize]
        [RequireActiveAccount]
        [RequireSystemAccount]
        [ProducesResponseType(typeof(SkillResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateSkill([FromBody] CreateSkillRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            try
            {
                var skill = await _skillService.CreateSkillAsync(request, accountId);
                return StatusCode(201, skill);
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

        /// <summary>Cập nhật tên kỹ năng trong danh mục toàn cục (Chỉ System Admin).</summary>
        [HttpPut("{skillId}")]
        [Authorize]
        [RequireActiveAccount]
        [RequireSystemAccount]
        [ProducesResponseType(typeof(SkillResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateSkill(int skillId, [FromBody] UpdateSkillRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            try
            {
                var skill = await _skillService.UpdateSkillAsync(skillId, request, accountId);
                return Ok(skill);
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

        /// <summary>Xóa mềm kỹ năng khỏi danh mục toàn cục (Chỉ System Admin, an toàn khi không có ai dùng).</summary>
        [HttpDelete("{skillId}")]
        [Authorize]
        [RequireActiveAccount]
        [RequireSystemAccount]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSkill(int skillId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            try
            {
                await _skillService.DeleteSkillAsync(skillId, accountId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
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
