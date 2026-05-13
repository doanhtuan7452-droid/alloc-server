namespace AllocServer.Interfaces.Commands
{
    /// <summary>
    /// Command Pattern — Marker interface để các Command class thực thi logout cùng 1 interface chung.
    /// RevokeSessionCommandHandler nhận ILogoutCommand và dispatch sang đúng logic dựa vào kiểu thực tế.
    /// </summary>
    public interface ILogoutCommand
    {
        /// <summary>AccountID trích từ JWT claim 'sub' — đảm bảo ownership</summary>
        int AccountId { get; }

        /// <summary>JTI (JWT ID) trích từ JWT claim 'jti' — dùng để denylist Access Token</summary>
        string JwtId { get; }

        /// <summary>Thời điểm hết hạn của Access Token — dùng để tính TTL cho Redis/Memory cache</summary>
        DateTime TokenExpiresAt { get; }
    }
}
