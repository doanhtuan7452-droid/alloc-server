using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("SubTasks")]
    public class SubTask
    {
        [Key]
        public int SubTaskID { get; set; }

        [Required]
        public int TaskID { get; set; }

        [ForeignKey("TaskID")]
        public ProjectTask? Task { get; set; }

        [Required]
        [StringLength(255)]
        public string SubTaskName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "To-do";

        public int? WorkspaceMemberID { get; set; }

        [ForeignKey("WorkspaceMemberID")]
        public WorkspaceMember? WorkspaceMember { get; set; }

        [Required]
        public int OrderIndex { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
