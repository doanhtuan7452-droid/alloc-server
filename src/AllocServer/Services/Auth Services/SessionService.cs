using AllocServer.Data;
using AllocServer.Interfaces.Auth;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Auth_Services
{
    public class SessionService : ISessionService
    {
        private readonly ApplicationDbContext _context;

        public SessionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AccountSession> CreateSessionAsync(int accountId, string refreshToken, string? deviceInfo, string? ipAddress, int expiresInDays)
        {
            var session = new AccountSession
            {
                AccountID = accountId,
                RefreshToken = refreshToken,
                DeviceInfo = deviceInfo,
                IPAddress = ipAddress,
                ExpiresAt = DateTime.UtcNow.AddDays(expiresInDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.AccountSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<AccountSession?> GetSessionAsync(string refreshToken)
        {
            return await _context.AccountSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
        }

        public async Task<bool> RevokeSessionAsync(string refreshToken)
        {
            var rowsAffected = await _context.AccountSessions
                .Where(s => s.RefreshToken == refreshToken && s.IsRevoked == false)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsRevoked, true));
            return rowsAffected > 0;
        }

        public async Task RevokeAllSessionsByAccountIdAsync(int accountId)
        {
            // Lấy tất cả session chưa bị revoke của account
            var activeSessions = await _context.AccountSessions
                .Where(s => s.AccountID == accountId && s.IsRevoked == false)
                .ToListAsync();

            if (!activeSessions.Any()) return;

            // Đánh dấu thu hồi tất cả — 1 lần SaveChanges (hiệu quả hơn từng cái)
            foreach (var session in activeSessions)
                session.IsRevoked = true;

            await _context.SaveChangesAsync();
        }
    }
}
