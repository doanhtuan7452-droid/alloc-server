using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Notifications
{
    public class RegisterDeviceTokenRequest
    {
        [Required]
        [StringLength(20)]
        [RegularExpression("^(iOS|Android|Web)$", ErrorMessage = "DeviceType must be iOS, Android, or Web")]
        public string DeviceType { get; set; } = string.Empty;

        [Required]
        [StringLength(1024)]
        public string DeviceToken { get; set; } = string.Empty;
    }
}
