using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    public class WorkspaceRole
    {
        [Key]
        public int WorkspaceRoleID { get; set; }

        public int? WorkspaceID { get; set; }
        
        [ForeignKey("WorkspaceID")]
        public Workspace Workspace { get; set; }

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; }

        public bool IsTemplate { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Soft delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}
