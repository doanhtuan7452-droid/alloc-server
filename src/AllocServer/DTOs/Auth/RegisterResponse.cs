namespace AllocServer.DTOs.Auth
{
    /// <summary>
    /// DTO trả về sau khi đăng ký tài khoản thành công hoặc thất bại
    /// </summary>
    public class RegisterResponse
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        /// <summary>ID của Account vừa tạo hoặc đã được link</summary>
        public int? AccountID { get; set; }

        public string? Email { get; set; }

        /// <summary>Local | Google | Microsoft</summary>
        public string? AuthType { get; set; }

        /// <summary>JWT Access Token để dùng ngay sau đăng ký</summary>
        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        public string? Message { get; set; }

        /// <summary>
        /// True nếu Google email trùng với tài khoản Local đã tồn tại → đã link thành công
        /// </summary>
        public bool IsLinked { get; set; } = false;
    }
}
