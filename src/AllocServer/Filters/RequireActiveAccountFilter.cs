using AllocServer.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;

namespace AllocServer.Filters
{
    public class RequireActiveAccountFilter : IAsyncActionFilter
    {
        public const string CurrentAccountIdItemKey = "CurrentAccountId";
        public const string CurrentAccountStatusItemKey = "CurrentAccountStatus";
        public const string CurrentAccountIsSystemAccountItemKey = "CurrentAccountIsSystemAccount";

        private readonly IAccountService _accountService;

        public RequireActiveAccountFilter(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var accountIdClaim = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!int.TryParse(accountIdClaim, out var accountId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Token khong hop le."
                });
                return;
            }

            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Tai khoan khong ton tai hoac da bi xoa."
                });
                return;
            }

            if (!string.Equals(account.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new
                {
                    message = "Tai khoan khong o trang thai hoat dong.",
                    accountStatus = account.AccountStatus
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            context.HttpContext.Items[CurrentAccountIdItemKey] = account.AccountID;
            context.HttpContext.Items[CurrentAccountStatusItemKey] = account.AccountStatus;
            context.HttpContext.Items[CurrentAccountIsSystemAccountItemKey] = account.IsSystemAccount ?? false;

            await next();
        }
    }
}
