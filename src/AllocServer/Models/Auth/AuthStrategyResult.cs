namespace AllocServer.Models.Auth
{
    /// <summary>
    /// Internal result object trả về từ Authentication Strategy.
    /// AuthFacade sẽ map model này sang DTO public (AuthResponse / RegisterResponse)
    /// để giữ nguyên API contract cho Controller / Client.
    /// </summary>
    public class AuthStrategyResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        // --- Account Info (dùng cho RegisterResponse) ---
        public int? AccountID { get; set; }
        public string? Email { get; set; }
        public string? AuthType { get; set; }

        // --- Token Pair ---
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }

        // --- Google Linking ---
        /// <summary>
        /// True nếu Google email trùng với tài khoản đã tồn tại → đã link thành công.
        /// Controller dùng field này để quyết định HTTP 200 OK hay 201 Created.
        /// </summary>
        public bool IsLinked { get; set; }
    }
}
