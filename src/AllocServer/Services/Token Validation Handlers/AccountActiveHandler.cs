using AllocServer.Contexts;
using AllocServer.DTOs.Auth;
using AllocServer.Interfaces.Auth;
using AllocServer.Resources;
using Microsoft.Extensions.Localization;

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
        private readonly IStringLocalizer<AuthResource> _localizer;

        public AccountActiveHandler(IAccountService accountService, IStringLocalizer<AuthResource> localizer)
        {
            _accountService = accountService;
            _localizer = localizer;
        }

        public override async Task<TokenValidationResult> HandleAsync(TokenValidationContext context)
        {
            // context.Session đã được Handler #1 đảm bảo không null
            var account = await _accountService.GetAccountByIdAsync(context.Session!.AccountID);

            if (account == null || account.IsDeleted)
            {
                return TokenValidationResult.Fail(_localizer["AccountNotFoundOrDeleted"]);
            }

            if (account.AccountStatus != "Active")
            {
                // To keep it simple, we use a single key for all non-active statuses or format it.
                // In a real scenario, we might want to pass the status as an argument to the localizer.
                return TokenValidationResult.Fail(_localizer["AccountNotActive", account.AccountStatus.ToLower()]);
            }

            // Gán Account vào context — đây là dữ liệu cuối cùng cần thiết để sinh token mới
            context.Account = account;

            // PASS → đây là handler cuối, PassToNextAsync sẽ trả về TokenValidationResult.Pass()
            return await PassToNextAsync(context);
        }
    }
}
