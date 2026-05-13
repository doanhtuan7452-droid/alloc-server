using AllocServer.DTOs.Auth;
using AllocServer.DTOs.Common;
using AllocServer.Interfaces.Facade;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthFacade _authFacade;

        public AuthController(IAuthFacade authFacade)
        {
            _authFacade = authFacade;
        }

        // =============================================
        // LOGIN
        // =============================================

        /// <summary>Đăng nhập bằng Email + Password (Local)</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authFacade.LoginAsync(request.Email, request.Password, request.DeviceInfo, ipAddress);

            if (!result.Success)
                return Unauthorized(new ApiResponse { Message = result.ErrorMessage });

            return Ok(result);
        }

        // =============================================
        // REGISTER — Strategy Pattern (qua Facade)
        // =============================================

        /// <summary>
        /// Đăng ký tài khoản Local bằng Email + Password.
        /// Sau khi thành công, tự động tạo profile (Resource) và trả về JWT token.
        /// </summary>
        [HttpPost("register/local")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterLocal([FromBody] LocalRegister request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authFacade.RegisterLocalAsync(request, request.DeviceInfo, ipAddress);

            if (!result.Success)
                return BadRequest(new ApiResponse { Message = result.ErrorMessage });

            return StatusCode(201, result);
        }

        /// <summary>
        /// Đăng ký / Đăng nhập bằng Google ID Token.
        /// - Nếu email Google chưa có trong hệ thống → Tạo tài khoản mới + profile.
        /// - Nếu email đã tồn tại (tài khoản Local) → Link tài khoản và trả về token (IsLinked = true).
        /// </summary>
        [HttpPost("register/google")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterGoogle([FromBody] GoogleRegister request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authFacade.RegisterGoogleAsync(request, request.DeviceInfo, ipAddress);

            if (!result.Success)
                return BadRequest(new ApiResponse { Message = result.ErrorMessage });

            // 201 Created nếu tạo mới, 200 OK nếu link vào account cũ
            return result.IsLinked
                ? Ok(result)
                : StatusCode(201, result);
        }

        // =============================================
        // REFRESH TOKEN — Chain of Responsibility (qua Facade)
        // =============================================

        /// <summary>
        /// Làm mới Access Token bằng Refresh Token còn hợp lệ.
        /// Áp dụng Refresh Token Rotation: token cũ bị thu hồi, trả về cặp token mới.
        /// Chuỗi xác thực: Tồn tại → Chưa thu hồi → Chưa hết hạn → Account còn hoạt động.
        /// </summary>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authFacade.RefreshTokenAsync(request.RefreshToken, request.DeviceInfo, ipAddress);

            if (!result.Success)
                return Unauthorized(new ApiResponse { Message = result.ErrorMessage });

            return Ok(result);
        }

        // =============================================
        // REVOKE / LOGOUT — Command Pattern (qua Facade)
        // Bảo mật: [Authorize] bắt buộc Bearer Token hợp lệ
        // AccountID và JTI đọc từ JWT claims — KHÔNG từ body (chống CSRF)
        // =============================================

        /// <summary>
        /// Local Logout — Đăng xuất khỏi thiết bị hiện tại.
        /// Thu hồi RefreshToken chỉ định + denylist Access Token hiện tại.
        /// Yêu cầu: Bearer Token hợp lệ trong header.
        /// </summary>
        [Authorize]
        [HttpPost("revoke/local")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LocalLogout([FromBody] LocalLogoutRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Trích thông tin từ JWT claims (Bearer Token đã được xác thực bởi middleware)
            var jti         = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var accountIdStr = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var expStr      = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            // Thiếu claim thiết yếu → token không hợp lệ
            if (string.IsNullOrEmpty(jti) || !int.TryParse(accountIdStr, out var accountId))
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });

            // Parse thời hạn token để tính TTL cho Denylist
            var tokenExpiresAt = long.TryParse(expStr, out var expUnix)
                ? DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime
                : DateTime.UtcNow;

            var result = await _authFacade.LocalLogoutAsync(
                request.RefreshToken, jti, tokenExpiresAt, accountId);

            if (!result.Success)
                return BadRequest(new ApiResponse { Message = result.Message });

            return Ok(new ApiResponse { Message = result.Message });
        }

        /// <summary>
        /// Global Logout — Đăng xuất khỏi tất cả thiết bị.
        /// Thu hồi TOÀN BỘ session của tài khoản + denylist Access Token hiện tại.
        /// Không cần body — AccountID lấy từ Bearer Token (chống CSRF).
        /// Lưu ý: Access Token từ thiết bị khác tự hết hiệu lực sau tối đa 20 phút.
        /// </summary>
        [Authorize]
        [HttpPost("revoke/global")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GlobalLogout()
        {
            var jti          = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var accountIdStr = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var expStr       = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

            if (string.IsNullOrEmpty(jti) || !int.TryParse(accountIdStr, out var accountId))
                return Unauthorized(new ApiResponse { Message = "Token không hợp lệ." });

            var tokenExpiresAt = long.TryParse(expStr, out var expUnix)
                ? DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime
                : DateTime.UtcNow;

            var result = await _authFacade.GlobalLogoutAsync(jti, tokenExpiresAt, accountId);

            if (!result.Success)
                return BadRequest(new ApiResponse { Message = result.Message });

            return Ok(new ApiResponse { Message = result.Message });
        }
    }
}
