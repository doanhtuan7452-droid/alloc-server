using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        public int RecipientID { get; set; }
        
        public int? ActorID { get; set; }

        [Required]
        [MaxLength(50)]
        public string NotificationType { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Message { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReferenceType { get; set; } = string.Empty;

        public int ReferenceID { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? MetadataJson { get; set; }

        [ForeignKey("RecipientID")]
        public virtual WorkspaceMember? Recipient { get; set; }

        [ForeignKey("ActorID")]
        public virtual WorkspaceMember? Actor { get; set; }
    }
}
