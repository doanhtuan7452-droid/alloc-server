using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Auth
{
    /// <summary>
    /// DTO nhận từ client khi gọi Local Logout (đăng xuất 1 thiết bị).
    /// Chỉ cần RefreshToken — AccountID và JTI lấy từ Bearer Token (chống CSRF).
    /// </summary>
    public class LocalLogoutRequest
    {
        [Required(ErrorMessage = "Refresh Token là bắt buộc.")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
