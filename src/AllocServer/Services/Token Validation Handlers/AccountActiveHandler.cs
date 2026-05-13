using AllocServer.Contexts;
using AllocServer.DTOs.Auth;
using AllocServer.Interfaces.Auth;

namespace AllocServer.Services.Token_Validation_Handlers
{
    /// <summary>
    /// Handler #4 (Cuối chain) — Kiểm tra Account liên kết với session còn Active không.
    /// Nhiệm vụ: Load Account từ DB bằng AccountID của session → kiểm tra IsDeleted và AccountStatus.
    /// Nếu account bị khóa/xóa → FAIL. Nếu OK → gán Account vào context → kết thúc chain thành công.
    /// </summary>
    public class AccountActiveHandler : BaseTokenValidationHandler
    {
        private readonly IAccountService _accountService;

        public AccountActiveHandler(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public override async Task<TokenValidationResult> HandleAsync(TokenValidationContext context)
        {
            // context.Session đã được Handler #1 đảm bảo không null
            var account = await _accountService.GetAccountByIdAsync(context.Session!.AccountID);

            if (account == null || account.IsDeleted)
            {
                return TokenValidationResult.Fail("Tài khoản không tồn tại hoặc đã bị xóa.");
            }

            if (account.AccountStatus != "Active")
            {
                return TokenValidationResult.Fail($"Tài khoản đang bị {account.AccountStatus.ToLower()}. Vui lòng liên hệ hỗ trợ.");
            }

            // Gán Account vào context — đây là dữ liệu cuối cùng cần thiết để sinh token mới
            context.Account = account;

            // PASS → đây là handler cuối, PassToNextAsync sẽ trả về TokenValidationResult.Pass()
            return await PassToNextAsync(context);
        }
    }
}
