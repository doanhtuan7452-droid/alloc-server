using AllocServer.Interfaces.Commands;

namespace AllocServer.Commands
{
    /// <summary>
    /// Command Pattern — Command cho Local Logout (đăng xuất 1 phiên/thiết bị cụ thể).
    /// Chứa đủ dữ liệu để RevokeSessionCommandHandler thực thi mà không cần query thêm param.
    /// </summary>
    public class LocalLogoutCommand : ILogoutCommand
    {
        /// <summary>Refresh Token của phiên cần thu hồi (do client gửi trong body)</summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>AccountID từ JWT claim 'sub' — dùng để xác minh session thuộc đúng user</summary>
        public int AccountId { get; set; }

        /// <summary>JTI từ JWT claim 'jti' — dùng để đưa vào Denylist</summary>
        public string JwtId { get; set; } = string.Empty;

        /// <summary>Thời điểm hết hạn của Access Token — dùng tính TTL cache</summary>
        public DateTime TokenExpiresAt { get; set; }
    }
}
