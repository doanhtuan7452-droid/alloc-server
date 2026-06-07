using AllocServer.Constants.Permissions;
using AllocServer.DTOs.Common;
using AllocServer.DTOs.WorkspaceMemberProfiles;
using AllocServer.Filters;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/workspaces/{workspaceId}/members/{memberId}/profile")]
    public class WorkspaceMemberProfilesController : ControllerBase
    {
        private readonly IWorkspaceMemberProfileService _profileService;

        public WorkspaceMemberProfilesController(IWorkspaceMemberProfileService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>Lay ho so (profile) cua mot thanh vien trong Workspace.</summary>
        [HttpGet]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.View)]
        [ProducesResponseType(typeof(WorkspaceMemberProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile(int workspaceId, int memberId)
        {
            var profile = await _profileService.GetProfileAsync(workspaceId, memberId);
            if (profile == null)
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay ho so cua nhan su trong Workspace." });
            }
            return Ok(profile);
        }

        /// <summary>Tao ho so moi cho mot thanh vien (neu chua co ho so mac dinh).</summary>
        [HttpPost]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.Manage)]
        [ProducesResponseType(typeof(WorkspaceMemberProfileResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateProfile(
            int workspaceId,
            int memberId,
            [FromBody] CreateMemberProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var profile = await _profileService.CreateProfileAsync(workspaceId, memberId, request);
                return StatusCode(201, profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Cap nhat hoac khoi phuc ho so cua thanh vien trong Workspace.</summary>
        [HttpPut]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.Manage)]
        [ProducesResponseType(typeof(WorkspaceMemberProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile(
            int workspaceId,
            int memberId,
            [FromBody] UpdateMemberProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var profile = await _profileService.UpdateProfileAsync(workspaceId, memberId, request);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Xoa mem ho so cua mot thanh vien trong Workspace.</summary>
        [HttpDelete]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize(MemberProfilePermissionIds.Manage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProfile(int workspaceId, int memberId)
        {
            var success = await _profileService.DeleteProfileAsync(workspaceId, memberId);
            if (!success)
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay ho so cua nhan su trong Workspace de xoa." });
            }
            return NoContent();
        }
    }
}
