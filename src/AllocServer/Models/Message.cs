using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        public int MessageID { get; set; }

        public int ConversationID { get; set; }
        public int SenderID { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsEdited { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        [ForeignKey("ConversationID")]
        public virtual Conversation? Conversation { get; set; }

        [ForeignKey("SenderID")]
        public virtual WorkspaceMember? Sender { get; set; }

        public virtual ICollection<MessageAsset> MessageAssets { get; set; } = new List<MessageAsset>();
    }
}
