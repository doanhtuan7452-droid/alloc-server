using AllocServer.DTOs.Notifications;
using AllocServer.Filters;
using AllocServer.Interfaces.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace AllocServer.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    [ServiceFilter(typeof(RequireActiveAccountFilter))]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                                 ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(accountIdClaim!);
        }

        /// <summary>Lay danh sach thong bao.</summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] GetNotificationsQuery query)
        {
            var accountId = GetCurrentAccountId();
            var response = await _notificationService.GetNotificationsAsync(accountId, query);
            return Ok(response);
        }

        /// <summary>Lay so luong thong bao chua doc.</summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var accountId = GetCurrentAccountId();
            var count = await _notificationService.GetUnreadCountAsync(accountId);
            return Ok(new { UnreadCount = count });
        }

        /// <summary>Lay chi tiet thong bao.</summary>
        [HttpGet("{notificationId}")]
        public async Task<IActionResult> GetNotificationDetail(int notificationId)
        {
            var accountId = GetCurrentAccountId();
            try
            {
                var response = await _notificationService.GetNotificationDetailAsync(accountId, notificationId);
                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Notification not found or access denied." });
            }
        }

        /// <summary>Danh dau thong bao da doc.</summary>
        [HttpPut("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var accountId = GetCurrentAccountId();
            try
            {
                await _notificationService.MarkAsReadAsync(accountId, notificationId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Notification not found or access denied." });
            }
        }

        /// <summary>Danh dau tat ca thong bao la da doc.</summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var accountId = GetCurrentAccountId();
            await _notificationService.MarkAllAsReadAsync(accountId);
            return NoContent();
        }

        /// <summary>Dang ky token thiet bi de nhan push notification.</summary>
        [HttpPost("device-tokens")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenRequest request)
        {
            var accountId = GetCurrentAccountId();
            await _notificationService.RegisterDeviceTokenAsync(accountId, request);
            return NoContent();
        }

        /// <summary>Huy dang ky token thiet bi.</summary>
        [HttpDelete("device-tokens")]
        public async Task<IActionResult> RevokeDeviceToken([FromBody] RevokeDeviceTokenRequest request)
        {
            var accountId = GetCurrentAccountId();
            await _notificationService.RevokeDeviceTokenAsync(accountId, request);
            return NoContent();
        }
    }
}
