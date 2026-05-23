using AllocServer.Constants;
using AllocServer.Contexts;
using AllocServer.DTOs.Auth;
using AllocServer.Interfaces.Auth;
using AllocServer.Interfaces.Commands;
using AllocServer.Interfaces.Facade;
using AllocServer.Commands;
using AllocServer.Models.Auth;
using AllocServer.Services.Token_Validation_Handlers;

namespace AllocServer.Services.Facade_Services
{
    /// <summary>
    /// Facade Pattern — Che giấu toàn bộ sự phức tạp của Authentication.
    /// Tích hợp với:
    ///   - Simple Factory + Strategy   (Login/Register) : IAuthStrategyFactory → LocalLogin / LocalRegister / GoogleAuth
    ///   - Chain of Responsibility     (Refresh Token)  : TokenExistsHandler → ... → AccountActiveHandler
    ///   - Command Pattern             (Logout/Revoke)  : LocalLogoutCommand, GlobalLogoutCommand → RevokeSessionCommandHandler
    /// </summary>
    public class AuthFacade : IAuthFacade
    {
        private readonly ITokenService _tokenService;
        private readonly ISessionService _sessionService;
        private readonly IConfiguration _configuration;

        // Simple Factory + Strategy Pattern
        private readonly IAuthStrategyFactory _authStrategyFactory;

        // Chain of Responsibility
        private readonly TokenExistsHandler _tokenExistsHandler;
        private readonly TokenNotRevokedHandler _tokenNotRevokedHandler;
        private readonly TokenNotExpiredHandler _tokenNotExpiredHandler;
        private readonly AccountActiveHandler _accountActiveHandler;

        // Command Pattern
        private readonly ILogoutCommandHandler _logoutCommandHandler;

        public AuthFacade(
            ITokenService tokenService,
            ISessionService sessionService,
            IConfiguration configuration,
            // Simple Factory + Strategy Pattern
            IAuthStrategyFactory authStrategyFactory,
            // Chain of Responsibility
            TokenExistsHandler tokenExistsHandler,
            TokenNotRevokedHandler tokenNotRevokedHandler,
            TokenNotExpiredHandler tokenNotExpiredHandler,
            AccountActiveHandler accountActiveHandler,
            // Command Pattern
            ILogoutCommandHandler logoutCommandHandler)
        {
            _tokenService = tokenService;
            _sessionService = sessionService;
            _configuration = configuration;
            _authStrategyFactory = authStrategyFactory;
            _tokenExistsHandler = tokenExistsHandler;
            _tokenNotRevokedHandler = tokenNotRevokedHandler;
            _tokenNotExpiredHandler = tokenNotExpiredHandler;
            _accountActiveHandler = accountActiveHandler;
            _logoutCommandHandler = logoutCommandHandler;
        }

        // =============================================
        // LOGIN — Simple Factory + Strategy Pattern
        // =============================================

        public async Task<AuthResponse> LoginAsync(string email, string password, string? deviceInfo, string? ipAddress)
        {
            var strategy = _authStrategyFactory.GetStrategy(AuthStrategyTypes.LocalLogin);
            var context = new AuthStrategyContext
            {
                Email = email,
                Password = password,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress
            };

            var result = await strategy.ExecuteAsync(context);

            // Map AuthStrategyResult → AuthResponse (public DTO)
            return new AuthResponse
            {
                Success = result.Success,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ErrorMessage = result.ErrorMessage
            };
        }

        // =============================================
        // REGISTER — Simple Factory + Strategy Pattern
        // =============================================

        public async Task<RegisterResponse> RegisterLocalAsync(LocalRegisterRequest request, string? deviceInfo, string? ipAddress)
        {
            var strategy = _authStrategyFactory.GetStrategy(AuthStrategyTypes.LocalRegister);
            var context = new AuthStrategyContext
            {
                Email = request.Email,
                Password = request.Password,
                FullName = request.FullName,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress
            };

            var result = await strategy.ExecuteAsync(context);

            // Map AuthStrategyResult → RegisterResponse (public DTO)
            return new RegisterResponse
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                AccountID = result.AccountID,
                Email = result.Email,
                AuthType = result.AuthType,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                IsLinked = result.IsLinked
            };
        }

        public async Task<RegisterResponse> RegisterGoogleAsync(GoogleRegisterRequest request, string? deviceInfo, string? ipAddress)
        {
            var strategy = _authStrategyFactory.GetStrategy(AuthStrategyTypes.GoogleAuth);
            var context = new AuthStrategyContext
            {
                IdToken = request.IdToken,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress
            };

            var result = await strategy.ExecuteAsync(context);

            // Map AuthStrategyResult → RegisterResponse (public DTO)
            return new RegisterResponse
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                AccountID = result.AccountID,
                Email = result.Email,
                AuthType = result.AuthType,
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                IsLinked = result.IsLinked
            };
        }

        // =============================================
        // REFRESH TOKEN — Chain of Responsibility
        // (Giữ nguyên 100% logic gốc — không thuộc phạm vi refactor này)
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
        // (Giữ nguyên 100% logic gốc — không thuộc phạm vi refactor này)
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
