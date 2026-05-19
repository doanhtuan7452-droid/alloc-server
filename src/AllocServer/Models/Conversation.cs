using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Conversations")]
    public class Conversation
    {
        [Key]
        public int ConversationID { get; set; }

        public int WorkspaceID { get; set; }
        public int? ProjectID { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        [MaxLength(100)]
        public string? ConversationKey { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        [ForeignKey("WorkspaceID")]
        public virtual Workspace? Workspace { get; set; }

        [ForeignKey("ProjectID")]
        public virtual Project? Project { get; set; }
    }
}
