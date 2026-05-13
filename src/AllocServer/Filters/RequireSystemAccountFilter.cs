using AllocServer.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;

namespace AllocServer.Filters
{
    public class RequireSystemAccountFilter : IAsyncActionFilter
    {
        private readonly IAccountService _accountService;

        public RequireSystemAccountFilter(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.Items.TryGetValue(
                    RequireActiveAccountFilter.CurrentAccountIsSystemAccountItemKey,
                    out var item)
                && item is bool isCurrentSystemAccount)
            {
                if (!isCurrentSystemAccount)
                {
                    context.Result = ForbiddenResult();
                    return;
                }

                await next();
                return;
            }

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

            if (account.IsSystemAccount != true)
            {
                context.Result = ForbiddenResult();
                return;
            }

            await next();
        }

        private static ObjectResult ForbiddenResult()
        {
            return new ObjectResult(new
            {
                message = "Tai khoan khong co quyen quan tri he thong."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
