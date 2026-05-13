namespace AllocServer.Contexts
{
    /// <summary>
    /// Context object chứa toàn bộ dữ liệu cần thiết cho quá trình đăng ký.
    /// Được truyền vào Strategy để tách biệt logic khỏi dữ liệu đầu vào.
    /// </summary>
    public class RegisterContext
    {
        // --- Thông tin chung ---
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public string AuthType { get; set; } = "Local";

        // --- Local Auth ---
        public string? Password { get; set; }

        // --- Google Auth ---
        /// <summary>Google ID Token từ Google Sign-In client</summary>
        public string? IdToken { get; set; }

        /// <summary>Google User ID (sub) — điền sau khi xác thực token</summary>
        public string? GoogleSub { get; set; }

        /// <summary>URL ảnh đại diện từ Google profile</summary>
        public string? AvatarUrl { get; set; }
    }
}
