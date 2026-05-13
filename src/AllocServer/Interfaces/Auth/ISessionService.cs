using AllocServer.Models;

namespace AllocServer.Interfaces.Auth
{
    public interface ISessionService
    {
        Task<AccountSession> CreateSessionAsync(int accountId, string refreshToken, string? deviceInfo, string? ipAddress, int expiresInDays);

        /// <summary>Thu hồi 1 session cụ thể bằng RefreshToken (Local Logout)</summary>
        Task<bool> RevokeSessionAsync(string refreshToken);

        Task<AccountSession?> GetSessionAsync(string refreshToken);

        /// <summary>Thu hồi TẤT CẢ session của một Account (Global Logout — đăng xuất mọi thiết bị)</summary>
        Task RevokeAllSessionsByAccountIdAsync(int accountId);
    }
}
