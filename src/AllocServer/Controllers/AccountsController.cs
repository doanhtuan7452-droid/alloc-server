using AllocServer.DTOs.Accounts;
using AllocServer.DTOs.Common;
using AllocServer.Filters;
using AllocServer.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace AllocServer.Controllers
{
    [ApiController]
    [Route("api/v1/accounts")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>Admin lay danh sach toan bo tai khoan trong he thong.</summary>
        [Authorize]
        [RequireActiveAccount]
        [RequireSystemAccount]
        [HttpGet]
        [ProducesResponseType(typeof(PagedAccountsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAccounts([FromQuery] GetAccountsQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _accountService.GetAccountsAsync(query);
            return Ok(result);
        }

        /// <summary>Lay thong tin tai khoan hien tai kem profile Resource.</summary>
        [Authorize]
        [RequireActiveAccount]
        [HttpGet("me")]
        [ProducesResponseType(typeof(AccountProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMe()
        {
            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            var accountProfile = await _accountService.GetCurrentAccountProfileAsync(accountId);
            if (accountProfile == null)
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay tai khoan hoac profile Resource." });
            }

            return Ok(accountProfile);
        }

        /// <summary>Cap nhat thong tin ca nhan cua tai khoan hien tai.</summary>
        [Authorize]
        [RequireActiveAccount]
        [HttpPut]
        [ProducesResponseType(typeof(AccountProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateAccountProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentAccountId(out var accountId))
            {
                return Unauthorized(new ApiResponse { Message = "Token khong hop le." });
            }

            var updatedProfile = await _accountService.UpdateCurrentAccountProfileAsync(accountId, request);
            if (updatedProfile == null)
            {
                return NotFound(new ApiResponse { Message = "Khong tim thay tai khoan hoac profile Resource." });
            }

            return Ok(updatedProfile);
        }

        private bool TryGetCurrentAccountId(out int accountId)
        {
            if (HttpContext.Items.TryGetValue(RequireActiveAccountFilter.CurrentAccountIdItemKey, out var item)
                && item is int currentAccountId)
            {
                accountId = currentAccountId;
                return true;
            }

            var accountIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(accountIdClaim, out accountId);
        }
    }
}
