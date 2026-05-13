using AllocServer.Models;

namespace AllocServer.Contexts
{
    /// <summary>
    /// Chain of Responsibility — Context object được truyền qua toàn bộ chuỗi Handler.
    /// Mỗi Handler sẽ đọc và có thể bổ sung dữ liệu vào context này trước khi pass sang Handler kế tiếp.
    /// </summary>
    public class TokenValidationContext
    {
        /// <summary>Refresh Token từ request của client</summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Được điền bởi TokenExistsHandler sau khi tìm thấy session trong DB.
        /// Các handler sau dùng dữ liệu này mà không cần query lại DB.
        /// </summary>
        public AccountSession? Session { get; set; }

        /// <summary>
        /// Được điền bởi AccountActiveHandler sau khi tải Account từ DB.
        /// AuthFacade dùng để tạo JWT mới sau khi chain kết thúc thành công.
        /// </summary>
        public Account? Account { get; set; }
    }
}
