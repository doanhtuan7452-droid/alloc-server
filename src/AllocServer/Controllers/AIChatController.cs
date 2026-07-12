using AllocServer.DTOs.AIChat;
using AllocServer.DTOs.Common;
using AllocServer.Exceptions;
using AllocServer.Filters;
using AllocServer.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/ai/chat")]
    public class AIChatController : ControllerBase
    {
        private readonly IPythonChatService _pythonChatService;

        public AIChatController(IPythonChatService pythonChatService)
        {
            _pythonChatService = pythonChatService;
        }

        /// <summary>Lay danh sach cuoc hoi thoai voi AI cua nguoi dung.</summary>
        [HttpGet("conversations")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(PythonConversationsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetConversations(
            [FromQuery, Range(1, 100)] int limit = 20,
            [FromQuery, Range(0, int.MaxValue)] int skip = 0)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var response = await _pythonChatService.GetConversationsAsync(accountId.ToString(), limit, skip);
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
            catch (Exception ex)
            {
                // Unhandled downstream errors will bubble up or can be handled as 500
                return StatusCode(500, new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Lay danh sach tin nhan trong cuoc hoi thoai AI.</summary>
        [HttpGet("conversations/{conversationId}/messages")]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(PythonMessagesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMessages(
            string conversationId,
            [FromQuery, Range(1, 100)] int limit = 50,
            [FromQuery, Range(0, int.MaxValue)] int skip = 0,
            [FromQuery, RegularExpression("^(asc|desc)$")] string order = "asc")
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return BadRequest(new ApiResponse { Message = "Conversation ID khong hop le." });
            }

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var response = await _pythonChatService.GetMessagesAsync(conversationId, accountId.ToString(), limit, skip, order);
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
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { Message = ex.Message });
            }
        }

        /// <summary>Gui tin nhan chat voi AI va luu tru lich su trong MongoDB.</summary>
        [HttpPost]
        [Authorize]
        [RequireActiveAccount]
        [ProducesResponseType(typeof(PythonChatQueryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Chat([FromBody] PythonChatQueryRequest request, System.Threading.CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            try
            {
                var response = await _pythonChatService.ChatAsync(accountId.ToString(), request, cancellationToken);
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
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { Message = ex.Message });
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
