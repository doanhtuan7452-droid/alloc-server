using AllocServer.DTOs.Common;
using AllocServer.DTOs.Conversations;
using AllocServer.DTOs.Messages;
using AllocServer.Filters;
using AllocServer.Interfaces.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _conversationService;

        public ConversationsController(IConversationService conversationService)
        {
            _conversationService = conversationService;
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

        [HttpPost("workspaces/{workspaceId}/conversations")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(ConversationDetailResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateConversation(
            int workspaceId,
            [FromBody] CreateConversationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            try
            {
                var response = await _conversationService.CreateConversationAsync(accountId, workspaceId, request);
                return StatusCode(201, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
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

        [HttpGet("workspaces/{workspaceId}/conversations")]
        [Authorize]
        [RequireActiveAccount]
        [WorkspaceAuthorize]
        [ProducesResponseType(typeof(List<ConversationListItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetWorkspaceConversations(int workspaceId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            try
            {
                var conversations = await _conversationService.GetWorkspaceConversationsAsync(accountId, workspaceId);
                return Ok(conversations);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }

        [HttpGet("conversations/{conversationId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(ConversationDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetConversationDetails(int conversationId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            try
            {
                var details = await _conversationService.GetConversationDetailsAsync(accountId, conversationId);
                return Ok(details);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }

        [HttpGet("conversations/{conversationId}/messages")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(List<MessageResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetConversationMessages(
            int conversationId,
            [FromQuery] GetConversationMessagesQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var messages = await _conversationService.GetConversationMessagesAsync(
                    accountId,
                    conversationId,
                    query);

                return Ok(messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }

        [HttpPost("conversations/{conversationId}/messages")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SendMessage(
            int conversationId,
            [FromBody] CreateMessageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var message = await _conversationService.SendMessageAsync(
                    accountId,
                    conversationId,
                    request);

                return StatusCode(201, message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }

        [HttpPut("conversations/{conversationId}/read")]
        [HttpPost("conversations/{conversationId}/read")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MarkConversationAsRead(int conversationId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });
            }

            try
            {
                await _conversationService.MarkConversationAsReadAsync(accountId, conversationId);
                return Ok(new ApiResponse { Message = "Đã đánh dấu đọc." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
        }
        [HttpPut("conversations/{conversationId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(ConversationDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RenameConversation(int conversationId, [FromBody] UpdateConversationNameRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });

            try
            {
                var response = await _conversationService.RenameConversationAsync(accountId, conversationId, request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
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

        [HttpDelete("conversations/{conversationId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteConversation(int conversationId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });

            try
            {
                await _conversationService.DeleteConversationAsync(accountId, conversationId);
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

        [HttpPost("conversations/{conversationId}/members")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(ConversationDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddMembers(int conversationId, [FromBody] AddConversationMembersRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });

            try
            {
                var response = await _conversationService.AddMembersAsync(accountId, conversationId, request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(409, new ApiResponse { Message = ex.Message });
            }
        }

        [HttpDelete("conversations/{conversationId}/members/{memberId}")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RemoveMember(int conversationId, int memberId)
        {
            if (!TryGetCurrentAccountId(out var accountId))
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });

            try
            {
                await _conversationService.RemoveMemberAsync(accountId, conversationId, memberId);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(409, new ApiResponse { Message = ex.Message });
            }
        }

    }
}
