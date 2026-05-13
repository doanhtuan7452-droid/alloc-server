using AllocServer.DTOs.Auth;
using AllocServer.Interfaces.Auth;
using AllocServer.Interfaces.Commands;
using AllocServer.Interfaces.Facade;
using AllocServer.Interfaces.Register;
using AllocServer.Commands;
using AllocServer.Contexts;
using AllocServer.Services.Register_Strategies;
using AllocServer.Services.Token_Validation_Handlers;

namespace AllocServer.Services.Facade_Services
{
    /// <summary>
    /// Facade Pattern — Che giấu toàn bộ sự phức tạp của Authentication.
    /// Tích hợp với:
    ///   - Strategy Pattern        (Register)      : LocalRegistrationStrategy, GoogleRegistrationStrategy
    ///   - Chain of Responsibility (Refresh Token) : TokenExistsHandler → ... → AccountActiveHandler
    ///   - Command Pattern         (Logout/Revoke) : LocalLogoutCommand, GlobalLogoutCommand → RevokeSessionCommandHandler
    /// </summary>
    public class AuthFacade : IAuthFacade
    {
        private readonly IAccountService _accountService;
        private readonly ITokenService _tokenService;
        private readonly ISessionService _sessionService;
        private readonly IConfiguration _configuration;

        // Strategy Pattern
        private readonly LocalRegistrationStrategy _localStrategy;
        private readonly GoogleRegistrationStrategy _googleStrategy;
        private readonly IRegisterStrategyContext _strategyContext;

        // Chain of Responsibility
        private readonly TokenExistsHandler _tokenExistsHandler;
        private readonly TokenNotRevokedHandler _tokenNotRevokedHandler;
        private readonly TokenNotExpiredHandler _tokenNotExpiredHandler;
        private readonly AccountActiveHandler _accountActiveHandler;

        // Command Pattern
        private readonly ILogoutCommandHandler _logoutCommandHandler;

        public AuthFacade(
            IAccountService accountService,
            ITokenService tokenService,
            ISessionService sessionService,
            IConfiguration configuration,
            // Strategy Pattern
            LocalRegistrationStrategy localStrategy,
            GoogleRegistrationStrategy googleStrategy,
            IRegisterStrategyContext strategyContext,
            // Chain of Responsibility
            TokenExistsHandler tokenExistsHandler,
            TokenNotRevokedHandler tokenNotRevokedHandler,
            TokenNotExpiredHandler tokenNotExpiredHandler,
            AccountActiveHandler accountActiveHandler,
            // Command Pattern
            ILogoutCommandHandler logoutCommandHandler)
        {
            _accountService = accountService;
            _tokenService = tokenService;
            _sessionService = sessionService;
            _configuration = configuration;
            _localStrategy = localStrategy;
            _googleStrategy = googleStrategy;
            _strategyContext = strategyContext;
            _tokenExistsHandler = tokenExistsHandler;
            _tokenNotRevokedHandler = tokenNotRevokedHandler;
            _tokenNotExpiredHandler = tokenNotExpiredHandler;
            _accountActiveHandler = accountActiveHandler;
            _logoutCommandHandler = logoutCommandHandler;
        }

        // =============================================
        // LOGIN
        // =============================================

        public async Task<AuthResponse> LoginAsync(string email, string password, string? deviceInfo, string? ipAddress)
        {
            var account = await _accountService.GetAccountByEmailAsync(email);
            if (account == null)
                return new AuthResponse { Success = false, ErrorMessage = "Tài khoản không tồn tại hoặc đã bị khóa." };

            if (account.AuthType != "Local")
                return new AuthResponse { Success = false, ErrorMessage = $"Tài khoản này đăng ký bằng {account.AuthType}. Vui lòng dùng phương thức đăng nhập tương ứng." };

            if (!_accountService.VerifyPassword(password, account.PasswordHash))
                return new AuthResponse { Success = false, ErrorMessage = "Mật khẩu không chính xác." };

            await _accountService.UpdateLastLoginAsync(account.AccountID);

            var accessToken = _tokenService.GenerateJwtToken(account);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenDays = double.Parse(
                _configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"] ?? "7");

            await _sessionService.CreateSessionAsync(
                account.AccountID, refreshToken, deviceInfo, ipAddress, (int)refreshTokenDays);

            return new AuthResponse { Success = true, AccessToken = accessToken, RefreshToken = refreshToken };
        }

        // =============================================
        // REGISTER — Strategy Pattern
        // =============================================

        public async Task<RegisterResponse> RegisterLocalAsync(LocalRegister request, string? deviceInfo, string? ipAddress)
        {
            _strategyContext.SetStrategy(_localStrategy);
            var context = new RegisterContext
            {
                Email = request.Email, Password = request.Password, FullName = request.FullName,
                DeviceInfo = deviceInfo, IpAddress = ipAddress, AuthType = "Local"
            };
            return await _strategyContext.ExecuteAsync(context);
        }

        public async Task<RegisterResponse> RegisterGoogleAsync(GoogleRegister request, string? deviceInfo, string? ipAddress)
        {
            _strategyContext.SetStrategy(_googleStrategy);
            var context = new RegisterContext
            {
                IdToken = request.IdToken, DeviceInfo = deviceInfo,
                IpAddress = ipAddress, AuthType = "Google"
            };
            return await _strategyContext.ExecuteAsync(context);
        }

        // =============================================
        // REFRESH TOKEN — Chain of Responsibility
        // =============================================

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? deviceInfo, string? ipAddress)
        {
            _tokenExistsHandler
                .SetNext(_tokenNotRevokedHandler)
                .SetNext(_tokenNotExpiredHandler)
                .SetNext(_accountActiveHandler);

            var validationContext = new TokenValidationContext { RefreshToken = refreshToken };
            var validationResult = await _tokenExistsHandler.HandleAsync(validationContext);

            if (!validationResult.IsValid)
                return new AuthResponse { Success = false, ErrorMessage = validationResult.ErrorMessage };

            var isRevoked = await _sessionService.RevokeSessionAsync(refreshToken);
            if (!isRevoked)
                return new AuthResponse { Success = false, ErrorMessage = "Refresh token không hợp lệ hoặc đã được sử dụng." };

            var newAccessToken = _tokenService.GenerateJwtToken(validationResult.Account!);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenDays = double.Parse(
                _configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"] ?? "7");

            await _sessionService.CreateSessionAsync(
                validationResult.Account!.AccountID, newRefreshToken,
                deviceInfo, ipAddress, (int)refreshTokenDays);

            return new AuthResponse { Success = true, AccessToken = newAccessToken, RefreshToken = newRefreshToken };
        }

        // =============================================
        // LOGOUT / REVOKE — Command Pattern
        // =============================================

        public async Task<LogoutResult> LocalLogoutAsync(
            string refreshToken, string jwtId, DateTime tokenExpiresAt, int accountId)
        {
            // Facade tạo Command → giao cho Handler thực thi (không biết logic bên trong)
            var command = new LocalLogoutCommand
            {
                RefreshToken = refreshToken,
                JwtId = jwtId,
                TokenExpiresAt = tokenExpiresAt,
                AccountId = accountId
            };
            return await _logoutCommandHandler.HandleAsync(command);
        }

        public async Task<LogoutResult> GlobalLogoutAsync(
            string jwtId, DateTime tokenExpiresAt, int accountId)
        {
            var command = new GlobalLogoutCommand
            {
                JwtId = jwtId,
                TokenExpiresAt = tokenExpiresAt,
                AccountId = accountId
            };
            return await _logoutCommandHandler.HandleAsync(command);
        }
    }
}
