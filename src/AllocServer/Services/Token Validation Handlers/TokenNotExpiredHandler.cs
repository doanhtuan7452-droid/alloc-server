using AllocServer.Contexts;
using AllocServer.DTOs.Auth;

namespace AllocServer.Services.Token_Validation_Handlers
{
    /// <summary>
    /// Handler #3 — Kiểm tra Refresh Token chưa hết hạn.
    /// Nhiệm vụ: So sánh session.ExpiresAt với DateTime.UtcNow → nếu đã qua thời hạn → FAIL.
    /// Không cần DB call, dùng session từ context.
    /// </summary>
    public class TokenNotExpiredHandler : BaseTokenValidationHandler
    {
        public override async Task<TokenValidationResult> HandleAsync(TokenValidationContext context)
        {
            // context.Session đã được Handler #1 đảm bảo không null
            if (context.Session!.ExpiresAt <= DateTime.UtcNow)
            {
                return TokenValidationResult.Fail("Refresh Token đã hết hạn. Vui lòng đăng nhập lại.");
            }

            // PASS → chuyển sang Handler kế tiếp
            return await PassToNextAsync(context);
        }
    }
}
