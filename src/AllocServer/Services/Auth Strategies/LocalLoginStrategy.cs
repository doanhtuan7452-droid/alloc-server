using AllocServer.Interfaces.Auth;
using AllocServer.Models.Auth;

namespace AllocServer.Services.Auth_Strategies
{
    /// <summary>
    /// Strategy Pattern — Chiến lược đăng nhập bằng Email + Password (Local).
    /// Logic giữ nguyên 100% từ AuthFacade.LoginAsync() gốc:
    ///   Kiểm tra account tồn tại → AuthType == "Local" → Verify password →
    ///   Cập nhật LastLogin → Sinh JWT + Refresh Token → Tạo Session
    /// </summary>
    public class LocalLoginStrategy : IAuthenticationStrategy
    {
        private readonly IAccountService _accountService;
        private readonly ITokenService _tokenService;
        private readonly ISessionService _sessionService;
        private readonly IConfiguration _configuration;

        public LocalLoginStrategy(
            IAccountService accountService,
            ITokenService tokenService,
            ISessionService sessionService,
            IConfiguration configuration)
        {
            _accountService = accountService;
            _tokenService = tokenService;
            _sessionService = sessionService;
            _configuration = configuration;
        }

        public async Task<AuthStrategyResult> ExecuteAsync(AuthStrategyContext context)
        {
            // 1. Kiểm tra account tồn tại
            var account = await _accountService.GetAccountByEmailAsync(context.Email!);
            if (account == null)
                return new AuthStrategyResult
                {
                    Success = false,
                    ErrorMessage = "Tài khoản không tồn tại hoặc đã bị khóa."
                };

            // 2. Kiểm tra AuthType phải là Local
            if (account.AuthType != "Local")
                return new AuthStrategyResult
                {
                    Success = false,
                    ErrorMessage = $"Tài khoản này đăng ký bằng {account.AuthType}. Vui lòng dùng phương thức đăng nhập tương ứng."
                };

            // 3. Verify password hash
            if (!_accountService.VerifyPassword(context.Password!, account.PasswordHash))
                return new AuthStrategyResult
                {
                    Success = false,
                    ErrorMessage = "Mật khẩu không chính xác."
                };

            // 4. Cập nhật thời gian đăng nhập cuối
            await _accountService.UpdateLastLoginAsync(account.AccountID);

            // 5. Sinh JWT Access Token
            var accessToken = _tokenService.GenerateJwtToken(account);

            // 6. Sinh Refresh Token và tạo Session
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenDays = double.Parse(
                _configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"] ?? "7");

            await _sessionService.CreateSessionAsync(
                account.AccountID, refreshToken, context.DeviceInfo, context.IpAddress, (int)refreshTokenDays);

            return new AuthStrategyResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
    }
}
