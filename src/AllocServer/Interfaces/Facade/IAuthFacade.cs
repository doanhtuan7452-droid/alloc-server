using AllocServer.DTOs.Auth;
using AllocServer.Models;

namespace AllocServer.Interfaces.Facade
{
    /// <summary>
    /// Facade Pattern — Giao diện duy nhất cho toàn bộ luồng Authentication.
    /// Controller chỉ cần biết interface này, không cần biết các sub-services bên trong.
    /// </summary>
    public interface IAuthFacade
    {
        // --- Login ---
        Task<AuthResponse> LoginAsync(string email, string password, string? deviceInfo, string? ipAddress);

        // --- Register (Strategy Pattern) ---
        Task<RegisterResponse> RegisterLocalAsync(LocalRegisterRequest request, string? deviceInfo, string? ipAddress);
        Task<RegisterResponse> RegisterGoogleAsync(GoogleRegisterRequest request, string? deviceInfo, string? ipAddress);

        // --- Refresh Token (Chain of Responsibility) ---
        Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? deviceInfo, string? ipAddress);

        // --- OTP Verification ---
        Task<bool> RequestOtpAsync(string email);
        Task<bool> VerifyOtpAsync(string email, string code);

        // --- Logout / Revoke (Command Pattern) ---
        /// <summary>Local Logout: thu hồi 1 phiên + denylist Access Token hiện tại</summary>
        Task<LogoutResult> LocalLogoutAsync(string refreshToken, string jwtId, DateTime tokenExpiresAt, int accountId);

        /// <summary>Global Logout: thu hồi tất cả phiên + denylist Access Token hiện tại</summary>
        Task<LogoutResult> GlobalLogoutAsync(string jwtId, DateTime tokenExpiresAt, int accountId);
    }
}

