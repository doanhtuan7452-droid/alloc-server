namespace AllocServer.Interfaces.Auth
{
    /// <summary>
    /// Interface cho dịch vụ quản lý JWT Denylist (Blocklist).
    /// Triển khai bằng IDistributedCache — tự động dùng In-Memory (Dev) hoặc Redis (Production).
    /// Mục tiêu: Vô hiệu hóa Access Token đã logout trong thời gian còn lại của token.
    /// </summary>
    public interface ITokenDenylistService
    {
        /// <summary>
        /// Đưa JWT ID vào denylist với TTL bằng thời gian còn lại của token.
        /// Sau khi TTL hết, entry tự động bị xóa (Redis/Memory tự dọn dẹp).
        /// </summary>
        Task AddToDenylistAsync(string jwtId, TimeSpan ttl);

        /// <summary>
        /// Kiểm tra JWT ID có trong denylist không.
        /// Được gọi bởi TokenDenylistMiddleware trên TỪNG request có Bearer Token.
        /// </summary>
        Task<bool> IsTokenDeniedAsync(string jwtId);
    }
}
