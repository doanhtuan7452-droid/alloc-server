using AllocServer.Models;

namespace AllocServer.DTOs.Auth
{
    /// <summary>
    /// Kết quả trả về sau khi chuỗi Chain of Responsibility hoàn tất.
    /// Nếu IsValid = false → một handler nào đó đã chặn và ErrorMessage chứa lý do.
    /// Nếu IsValid = true → toàn bộ chuỗi passed, Session và Account được điền đầy đủ.
    /// </summary>
    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Session hợp lệ (chỉ có giá trị khi IsValid = true)</summary>
        public AccountSession? Session { get; set; }

        /// <summary>Account hợp lệ (chỉ có giá trị khi IsValid = true)</summary>
        public Account? Account { get; set; }

        // ---- Factory methods để tạo result gọn hơn ----

        /// <summary>Tạo result thất bại với lý do cụ thể</summary>
        public static TokenValidationResult Fail(string errorMessage) =>
            new() { IsValid = false, ErrorMessage = errorMessage };

        /// <summary>Tạo result thành công khi toàn bộ chain đã pass</summary>
        public static TokenValidationResult Pass(AccountSession session, Account account) =>
            new() { IsValid = true, Session = session, Account = account };
    }
}
