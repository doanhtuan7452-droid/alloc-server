using AllocServer.Commands;
using AllocServer.Interfaces.Commands;
using AllocServer.DTOs.Auth;
using AllocServer.Interfaces.Auth;

namespace AllocServer.Services.Command_Handlers
{
    /// <summary>
    /// Command Pattern — Concrete Handler chứa toàn bộ logic xử lý Logout.
    /// Nhận ILogoutCommand và dispatch sang đúng luồng bằng C# pattern matching.
    /// 
    /// Local Logout:
    ///   1. Tìm session bằng RefreshToken
    ///   2. Xác minh session.AccountID == command.AccountId (chống giả mạo)
    ///   3. IsRevoked = 1 → SaveChangesAsync()
    ///   4. Tính TTL còn lại → đưa JTI vào IDistributedCache Denylist
    ///
    /// Global Logout:
    ///   1. RevokeAllSessionsByAccountIdAsync → tất cả sessions IsRevoked = 1
    ///   2. Đưa JTI của Access Token hiện tại vào Denylist
    /// </summary>
    public class RevokeSessionCommandHandler : ILogoutCommandHandler
    {
        private readonly ISessionService _sessionService;
        private readonly ITokenDenylistService _denylistService;

        public RevokeSessionCommandHandler(
            ISessionService sessionService,
            ITokenDenylistService denylistService)
        {
            _sessionService = sessionService;
            _denylistService = denylistService;
        }

        public async Task<LogoutResult> HandleAsync(ILogoutCommand command)
        {
            // Dispatch sang đúng handler dựa trên kiểu Command (C# pattern matching)
            return command switch
            {
                LocalLogoutCommand local   => await HandleLocalAsync(local),
                GlobalLogoutCommand global => await HandleGlobalAsync(global),
                _ => LogoutResult.Fail("Loại lệnh logout không được hỗ trợ.")
            };
        }

        // ============================================================
        // LOCAL LOGOUT — Thu hồi 1 phiên cụ thể
        // ============================================================

        private async Task<LogoutResult> HandleLocalAsync(LocalLogoutCommand cmd)
        {
            // Bước 1: Tìm session trong DB bằng RefreshToken
            var session = await _sessionService.GetSessionAsync(cmd.RefreshToken);

            if (session == null)
            {
                // Session không tồn tại hoặc đã bị revoke — coi như logout thành công (idempotent)
                return LogoutResult.Ok("Đã đăng xuất thành công.");
            }

            // Bước 2: Xác minh session thuộc đúng người dùng gọi API
            // Chống tấn công: User A không thể logout session của User B
            if (session.AccountID != cmd.AccountId)
            {
                return LogoutResult.Fail("Bạn không có quyền thu hồi phiên này.");
            }

            // Bước 3: Đánh dấu IsRevoked = 1 trong DB
            await _sessionService.RevokeSessionAsync(cmd.RefreshToken);

            // Bước 4: Đưa Access Token hiện tại vào Denylist (xử lý tính phi trạng thái JWT)
            await DenylistAccessTokenAsync(cmd.JwtId, cmd.TokenExpiresAt);

            return LogoutResult.Ok("Đã đăng xuất thành công.");
        }

        // ============================================================
        // GLOBAL LOGOUT — Thu hồi TẤT CẢ phiên của tài khoản
        // ============================================================

        private async Task<LogoutResult> HandleGlobalAsync(GlobalLogoutCommand cmd)
        {
            // Bước 1: Thu hồi TẤT CẢ Refresh Token của account trong DB
            await _sessionService.RevokeAllSessionsByAccountIdAsync(cmd.AccountId);

            // Bước 2: Denylist Access Token hiện tại
            // Lưu ý: Access Token của các thiết bị khác sẽ tự hết hạn sau tối đa 20 phút
            // (đã giảm từ 60 → 20 phút để giảm cửa sổ tấn công)
            await DenylistAccessTokenAsync(cmd.JwtId, cmd.TokenExpiresAt);

            return LogoutResult.Ok("Đã đăng xuất khỏi tất cả thiết bị thành công.");
        }

        // ============================================================
        // HELPER — Tính TTL và đưa vào Denylist
        // ============================================================

        private async Task DenylistAccessTokenAsync(string jwtId, DateTime tokenExpiresAt)
        {
            if (string.IsNullOrEmpty(jwtId)) return;

            // Tính thời gian còn lại của Access Token
            var ttl = tokenExpiresAt - DateTime.UtcNow;

            // Chỉ denylist nếu token chưa hết hạn (có ý nghĩa)
            if (ttl > TimeSpan.Zero)
            {
                await _denylistService.AddToDenylistAsync(jwtId, ttl);
            }
        }
    }
}
