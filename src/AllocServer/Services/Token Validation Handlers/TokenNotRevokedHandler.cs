using AllocServer.Contexts;
using AllocServer.DTOs.Auth;
using AllocServer.Resources;
using Microsoft.Extensions.Localization;

namespace AllocServer.Services.Token_Validation_Handlers
{
    /// <summary>
    /// Handler #2 — Kiểm tra Refresh Token chưa bị thu hồi (Revoke).
    /// Nhiệm vụ: Đọc session.IsRevoked từ context (đã được Handler #1 điền) → nếu đã bị revoke → FAIL.
    /// Không cần DB call vì session đã được load sẵn từ Handler trước.
    /// </summary>
    public class TokenNotRevokedHandler : BaseTokenValidationHandler
    {
        private readonly IStringLocalizer<AuthResource> _localizer;

        public TokenNotRevokedHandler(IStringLocalizer<AuthResource> localizer)
        {
            _localizer = localizer;
        }

        public override async Task<TokenValidationResult> HandleAsync(TokenValidationContext context)
        {
            // context.Session đã được Handler #1 đảm bảo không null
            if (context.Session!.IsRevoked == true)
            {
                return TokenValidationResult.Fail(_localizer["TokenRevoked"]);
            }

            // PASS → chuyển sang Handler kế tiếp
            return await PassToNextAsync(context);
        }
    }
}
