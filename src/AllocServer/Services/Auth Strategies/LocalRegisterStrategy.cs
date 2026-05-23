using AllocServer.Interfaces.Auth;
using AllocServer.Models;
using AllocServer.Models.Auth;

namespace AllocServer.Services.Auth_Strategies
{
    /// <summary>
    /// Strategy Pattern — Chiến lược đăng ký bằng Email + Password (Local).
    /// Logic giữ nguyên 100% từ LocalRegistrationStrategy.RegisterAsync() gốc:
    ///   Kiểm tra email trùng → Hash password → Tạo Account → Tạo Resource → Sinh token → Tạo Session
    /// </summary>
    public class LocalRegisterStrategy : IAuthenticationStrategy
    {
        private readonly IAccountService _accountService;
        private readonly ITokenService _tokenService;
        private readonly ISessionService _sessionService;
        private readonly IConfiguration _configuration;

        public LocalRegisterStrategy(
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
            // 1. Kiểm tra email đã tồn tại chưa
            var emailExists = await _accountService.IsEmailExistsAsync(context.Email!);
            if (emailExists)
            {
                return new AuthStrategyResult
                {
                    Success = false,
                    ErrorMessage = "Email này đã được sử dụng. Vui lòng chọn email khác."
                };
            }

            // 2. Hash mật khẩu bằng BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(context.Password);

            // 3. Tạo Account mới
            var newAccount = new Account
            {
                Email = context.Email!,
                PasswordHash = passwordHash,
                AuthType = "Local",
                IsEmailVerified = false,   // Cần xác nhận email sau (có thể thêm email verification sau)
                AccountStatus = "Active",
                IsSystemAccount = false
            };

            var createdAccount = await _accountService.CreateAccountAsync(newAccount);

            // 4. Tự động tạo Resource (profile) cho tài khoản mới
            var newResource = new Resource
            {
                AccountID = createdAccount.AccountID,
                FullName = context.FullName ?? string.Empty,
                Timezone = "UTC"
            };

            await _accountService.CreateResourceAsync(newResource);

            // 5. Sinh JWT Access Token
            var accessToken = _tokenService.GenerateJwtToken(createdAccount);

            // 6. Sinh Refresh Token
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenDays = double.Parse(
                _configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"] ?? "7");

            // 7. Lưu Session vào DB
            await _sessionService.CreateSessionAsync(
                createdAccount.AccountID, refreshToken,
                context.DeviceInfo, context.IpAddress,
                (int)refreshTokenDays);

            return new AuthStrategyResult
            {
                Success = true,
                AccountID = createdAccount.AccountID,
                Email = createdAccount.Email,
                AuthType = "Local",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsLinked = false
            };
        }
    }
}
