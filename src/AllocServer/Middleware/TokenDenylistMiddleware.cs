using AllocServer.Interfaces.Auth;
using System.IdentityModel.Tokens.Jwt;

namespace AllocServer.Middleware
{
    /// <summary>
    /// Middleware kiểm tra JWT Denylist trên từng request có Bearer Token.
    /// Chạy sau UseAuthentication() — lúc này User.Claims đã được điền từ JWT.
    ///
    /// Luồng:
    ///   1. Request đến → UseAuthentication() validate JWT signature, claims...
    ///   2. TokenDenylistMiddleware → trích JTI từ User.Claims
    ///   3. Gọi ITokenDenylistService.IsTokenDeniedAsync(jti)
    ///   4. Nếu bị denylist → trả 401 ngay, dừng pipeline
    ///   5. Nếu không → tiếp tục → UseAuthorization() → Controller
    ///
    /// Note: ITokenDenylistService inject qua InvokeAsync() (không qua constructor)
    /// để hỗ trợ Scoped lifetime trong Singleton middleware.
    /// </summary>
    public class TokenDenylistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenDenylistMiddleware> _logger;

        public TokenDenylistMiddleware(RequestDelegate next, ILogger<TokenDenylistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenDenylistService denylistService)
        {
            // Chỉ kiểm tra nếu request có Bearer Token và đã authenticate thành công
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Trích JTI từ claims (MapInboundClaims = false → giữ tên gốc "jti")
                var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    var isDenied = await denylistService.IsTokenDeniedAsync(jti);

                    if (isDenied)
                    {
                        _logger.LogWarning("Blocked denied JWT. JTI: {Jti}, IP: {IP}",
                            jti, context.Connection.RemoteIpAddress);

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = "Access Token đã bị thu hồi. Vui lòng đăng nhập lại."
                        });
                        return; // Dừng pipeline — không gọi controller
                    }
                }
            }

            // Token hợp lệ (không trong denylist) → tiếp tục pipeline bình thường
            await _next(context);
        }
    }
}
