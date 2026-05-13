using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Auth
{
    /// <summary>
    /// DTO nhận từ client khi yêu cầu làm mới Access Token
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh Token là bắt buộc.")]
        public string RefreshToken { get; set; } = string.Empty;

        public string? DeviceInfo { get; set; }
    }
}
