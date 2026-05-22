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

        /// <summary>Dang nhap bang Email + Password (Local).</summary>
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
        /// Dang ky tai khoan Local bang Email + Password.
        /// Sau khi thanh cong, tu dong tao profile (Resource) va tra ve JWT token.
        /// </summary>
        [HttpPost("register/local")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterLocal([FromBody] LocalRegisterRequest request)
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
        /// Dang ky / Dang nhap bang Google ID Token.
        /// - Neu email Google chua co trong he thong -> Tao tai khoan moi + profile.
        /// - Neu email da ton tai (tai khoan Local) -> Link tai khoan va tra ve token (IsLinked = true).
        /// </summary>
        [HttpPost("register/google")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterGoogle([FromBody] GoogleRegisterRequest request)
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
        /// Lam moi Access Token bang Refresh Token con hop le.
        /// Ap dung Refresh Token Rotation: token cu bi thu hoi, tra ve cap token moi.
        /// Chuoi xac thuc: Ton tai -> Chua thu hoi -> Chua het han -> Account con hoat dong.
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
        /// Local Logout - Dang xuat khoi thiet bi hien tai.
        /// Thu hoi RefreshToken chi dinh + denylist Access Token hien tai.
        /// Yeu cau: Bearer Token hop le trong header.
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
        /// Global Logout - Dang xuat khoi tat ca thiet bi.
        /// Thu hoi TOAN BO session cua tai khoan + denylist Access Token hien tai.
        /// Khong can body - AccountID lay tu Bearer Token (chong CSRF).
        /// Luu y: Access Token tu thiet bi khac tu het hieu luc sau toi da 20 phut.
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
