using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("TaskAssignees")]
    public class TaskAssignee
    {
        [Required]
        public int TaskID { get; set; }

        [ForeignKey("TaskID")]
        public ProjectTask? Task { get; set; }

        [Required]
        public int WorkspaceMemberID { get; set; }

        [ForeignKey("WorkspaceMemberID")]
        public WorkspaceMember? WorkspaceMember { get; set; }

        [Required]
        [StringLength(20)]
        public string AssigneeType { get; set; } = "Assignee";

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
