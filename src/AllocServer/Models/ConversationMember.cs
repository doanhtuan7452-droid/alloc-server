using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("ConversationMembers")]
    public class ConversationMember
    {
        public int ConversationID { get; set; }
        public int MemberID { get; set; }
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ConversationID")]
        public virtual Conversation? Conversation { get; set; }

        [ForeignKey("MemberID")]
        public virtual WorkspaceMember? WorkspaceMember { get; set; }
    }
}
