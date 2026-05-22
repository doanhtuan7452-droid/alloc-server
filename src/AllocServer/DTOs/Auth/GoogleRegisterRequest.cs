using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Auth
{
    /// <summary>
    /// DTO dùng cho đăng ký / đăng nhập qua Google (ID Token từ Google Sign-In)
    /// </summary>
    public class GoogleRegisterRequest
    {
        [Required(ErrorMessage = "Google ID Token là bắt buộc.")]
        public string IdToken { get; set; } = string.Empty;

        public string? DeviceInfo { get; set; }
    }
}
