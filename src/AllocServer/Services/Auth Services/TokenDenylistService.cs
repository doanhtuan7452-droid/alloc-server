using AllocServer.Interfaces.Auth;
using Microsoft.Extensions.Caching.Distributed;

namespace AllocServer.Services.Auth_Services
{
    /// <summary>
    /// Triển khai ITokenDenylistService dùng IDistributedCache — interface thống nhất của ASP.NET Core.
    /// 
    /// - Development : Program.cs gọi AddDistributedMemoryCache()  → chạy In-Memory, không cần Redis
    /// - Production  : Program.cs gọi AddStackExchangeRedisCache() → tự động dùng Redis thật
    /// 
    /// Class này KHÔNG thay đổi khi swap môi trường — chỉ cần đổi registration trong Program.cs.
    /// </summary>
    public class TokenDenylistService : ITokenDenylistService
    {
        private readonly IDistributedCache _cache;

        // Prefix key trong cache để tránh trùng với các key khác
        private const string KeyPrefix = "jwt_denylist:";

        public TokenDenylistService(IDistributedCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Đưa JWT ID vào denylist với TTL = thời gian còn lại của token.
        /// Dev: lưu vào In-Memory. Production: lưu vào Redis với key "jwt_denylist:{jwtId}".
        /// Sau khi TTL hết → entry tự xóa, không cần dọn dẹp thủ công.
        /// </summary>
        public async Task AddToDenylistAsync(string jwtId, TimeSpan ttl)
        {
            // Nếu token đã hết hạn rồi thì không cần denylist (vô nghĩa)
            if (ttl <= TimeSpan.Zero) return;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            // Value "1" — chỉ cần biết key tồn tại, không quan tâm value
            await _cache.SetStringAsync($"{KeyPrefix}{jwtId}", "1", options);
        }

        /// <summary>
        /// Kiểm tra JWT ID có bị denylist không.
        /// Được gọi bởi TokenDenylistMiddleware trên từng authenticated request.
        /// </summary>
        public async Task<bool> IsTokenDeniedAsync(string jwtId)
        {
            var value = await _cache.GetStringAsync($"{KeyPrefix}{jwtId}");
            return value != null; // Tồn tại key → token bị thu hồi
        }
    }
}
