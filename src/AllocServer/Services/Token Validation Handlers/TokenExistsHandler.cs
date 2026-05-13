using AllocServer.Contexts;
using AllocServer.DTOs.Auth;
using AllocServer.Interfaces.Auth;

namespace AllocServer.Services.Token_Validation_Handlers
{
    /// <summary>
    /// Handler #1 — Kiểm tra Refresh Token có tồn tại trong DB không.
    /// Nhiệm vụ: Query DB tìm session → nếu không tìm thấy → FAIL ngay.
    /// Nếu tìm thấy → gán session vào context để các handler sau sử dụng → PASS sang Handler #2.
    /// </summary>
    public class TokenExistsHandler : BaseTokenValidationHandler
    {
        private readonly ISessionService _sessionService;

        public TokenExistsHandler(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public override async Task<TokenValidationResult> HandleAsync(TokenValidationContext context)
        {
            var session = await _sessionService.GetSessionAsync(context.RefreshToken);

            if (session == null)
            {
                // Token không tồn tại trong DB → dừng chain, trả về lỗi
                return TokenValidationResult.Fail("Refresh Token không hợp lệ hoặc không tồn tại.");
            }

            // Gán session vào context để các handler sau không cần query lại DB
            context.Session = session;

            // PASS → chuyển sang Handler kế tiếp
            return await PassToNextAsync(context);
        }
    }
}
