namespace AllocServer.Models.Auth
{
    /// <summary>
    /// Context object chứa toàn bộ dữ liệu đầu vào cho Authentication Strategy.
    /// Không chứa StrategyType — Factory đã chọn Strategy bằng GetStrategy() rồi.
    /// </summary>
    public class AuthStrategyContext
    {
        // --- Thông tin chung ---
        public string? Email { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }

        // --- Local Auth ---
        public string? Password { get; set; }
        public string? FullName { get; set; }

        // --- Google Auth ---
        /// <summary>Google ID Token từ Google Sign-In client</summary>
        public string? IdToken { get; set; }
    }
}
