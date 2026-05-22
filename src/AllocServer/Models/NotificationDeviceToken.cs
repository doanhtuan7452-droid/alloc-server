using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("NotificationDeviceTokens")]
    public class NotificationDeviceToken
    {
        [Key]
        public int TokenID { get; set; }

        public int AccountID { get; set; }

        [MaxLength(20)]
        public string? DeviceType { get; set; }

        [Required]
        [MaxLength(1024)]
        public string DeviceToken { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public DateTime? RevokedAt { get; set; }

        public string? LastError { get; set; }

        public int FailureCount { get; set; } = 0;

        [ForeignKey("AccountID")]
        public virtual Account? Account { get; set; }
    }
}
