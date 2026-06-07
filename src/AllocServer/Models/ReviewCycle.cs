using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    public class ReviewCycle
    {
        [Key]
        public int CycleID { get; set; }

        [Required]
        public int WorkspaceID { get; set; }

        [ForeignKey("WorkspaceID")]
        public Workspace Workspace { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string CycleName { get; set; } = null!;

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // 'Draft', 'Active', 'Completed', 'Cancelled'

        [Required]
        public int CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public WorkspaceMember Creator { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}
