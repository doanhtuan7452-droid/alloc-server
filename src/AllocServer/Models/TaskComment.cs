using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("TaskComments")]
    public class TaskComment
    {
        [Key]
        public int CommentID { get; set; }

        [Required]
        public int TaskID { get; set; }

        [ForeignKey("TaskID")]
        public ProjectTask? Task { get; set; }

        [Required]
        public int MemberID { get; set; }

        [ForeignKey("MemberID")]
        public WorkspaceMember? WorkspaceMember { get; set; }

        public int? ParentCommentID { get; set; }

        [ForeignKey("ParentCommentID")]
        public TaskComment? ParentComment { get; set; }

        public ICollection<TaskComment> Replies { get; set; } = new List<TaskComment>();

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
