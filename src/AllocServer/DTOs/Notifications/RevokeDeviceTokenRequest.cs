using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Notifications
{
    public class RevokeDeviceTokenRequest
    {
        [Required]
        [StringLength(1024)]
        public string DeviceToken { get; set; } = string.Empty;
    }
}
