using AllocServer.Constants.Permissions;
using AllocServer.Data;
using AllocServer.DTOs.Common;
using AllocServer.DTOs.WorkspaceMemberProfiles;
using AllocServer.Filters;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/workspaces/{workspaceId}/review-cycles")]
    public class ReviewCyclesController : ControllerBase
    {
        private readonly IWorkspaceMemberProfileService _profileService;
        private readonly ApplicationDbContext _context;

        public ReviewCyclesController(IWorkspaceMemberProfileService profileService, ApplicationDbContext context)
        {
            _profileService = profileService;
            _context = context;
        }

        /// <summary>Lay tat ca chu ky danh gia trong Workspace.</summary>
        [HttpGet]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.View)]
        [ProducesResponseType(typeof(List<ReviewCycleResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReviewCycles(int workspaceId)
        {
            var cycles = await _profileService.GetReviewCyclesAsync(workspaceId);
            return Ok(cycles);
        }

        /// <summary>Tao chu ky danh gia moi (Draft).</summary>
        [HttpPost]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.Manage)]
        [ProducesResponseType(typeof(ReviewCycleResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateReviewCycle(int workspaceId, [FromBody] CreateReviewCycleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            var member = await _context.WorkspaceMembers
                .AsNoTracking()
                .Include(m => m.Resource)
                .FirstOrDefaultAsync(m => m.Resource.AccountID == accountId && m.WorkspaceID == workspaceId && m.Status == "Active");

            if (member == null)
            {
                return BadRequest(new ApiResponse { Message = "Nguoi dung hien tai khong thuoc Workspace nay." });
            }

            try
            {
                var cycle = await _profileService.CreateReviewCycleAsync(workspaceId, request, member.WorkspaceMemberID);
                return StatusCode(201, cycle);
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

        /// <summary>Bat dau chu ky danh gia (Draft -> Active).</summary>
        [HttpPost("{cycleId}/start")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.Manage)]
        [ProducesResponseType(typeof(ReviewCycleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StartReviewCycle(int workspaceId, int cycleId)
        {
            try
            {
                var cycle = await _profileService.StartReviewCycleAsync(workspaceId, cycleId);
                return Ok(cycle);
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

        /// <summary>Hoan thanh chu ky danh gia (Active -> Completed) va kich hoat tinh lai diem.</summary>
        [HttpPost("{cycleId}/complete")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.Manage)]
        [ProducesResponseType(typeof(ReviewCycleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteReviewCycle(int workspaceId, int cycleId)
        {
            var cycleRecord = await _context.ReviewCycles
                .AsNoTracking()
                .FirstOrDefaultAsync(rc => rc.CycleID == cycleId && rc.WorkspaceID == workspaceId && !rc.IsDeleted);

            if (cycleRecord == null)
            {
                return NotFound(new ApiResponse { Message = "ReviewCycleNotFound" });
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (today < cycleRecord.EndDate)
            {
                if (!TryGetCurrentAccountId(out var accountId))
                {
                    return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
                }

                var member = await _context.WorkspaceMembers
                    .AsNoTracking()
                    .Include(m => m.WorkspaceRole)
                    .FirstOrDefaultAsync(m => m.Resource.AccountID == accountId && m.WorkspaceID == workspaceId && m.Status == "Active");

                if (member == null || member.WorkspaceRole?.RoleName != "Owner")
                {
                    return StatusCode(403, new ApiResponse { Message = "Chi co Owner moi duoc phep hoan thanh chu ky truoc han." });
                }
            }

            try
            {
                var cycle = await _profileService.CompleteReviewCycleAsync(workspaceId, cycleId);
                return Ok(cycle);
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

        /// <summary>Lay tat ca danh gia trong chu ky.</summary>
        [HttpGet("{cycleId}/evaluations")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.View)]
        [ProducesResponseType(typeof(List<MemberEvaluationResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvaluations(int workspaceId, int cycleId)
        {
            var evals = await _profileService.GetMemberEvaluationsAsync(workspaceId, cycleId);
            return Ok(evals);
        }

        /// <summary>Nop danh gia cho mot thanh vien trong chu ky.</summary>
        [HttpPost("{cycleId}/evaluations")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.View)]
        [ProducesResponseType(typeof(MemberEvaluationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitEvaluation(int workspaceId, int cycleId, [FromBody] SubmitEvaluationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var eval = await _profileService.SubmitMemberEvaluationAsync(workspaceId, cycleId, request);
                return Ok(eval);
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
