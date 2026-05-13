using AllocServer.Interfaces.Commands;

namespace AllocServer.Commands
{
    /// <summary>
    /// Command Pattern — Command cho Global Logout (đăng xuất khỏi tất cả thiết bị).
    /// Không cần RefreshToken — handler sẽ thu hồi TẤT CẢ session của AccountId.
    /// </summary>
    public class GlobalLogoutCommand : ILogoutCommand
    {
        /// <summary>AccountID từ JWT claim 'sub' — dùng để thu hồi mọi session</summary>
        public int AccountId { get; set; }

        /// <summary>JTI từ JWT claim 'jti' — dùng để đưa Access Token hiện tại vào Denylist</summary>
        public string JwtId { get; set; } = string.Empty;

        /// <summary>Thời điểm hết hạn của Access Token — dùng tính TTL cache</summary>
        public DateTime TokenExpiresAt { get; set; }
    }
}
