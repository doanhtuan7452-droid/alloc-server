using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    public class WorkspaceMember
    {
        [Key]
        public int WorkspaceMemberID { get; set; }

        [Required]
        public int WorkspaceID { get; set; }
        
        [ForeignKey("WorkspaceID")]
        public Workspace Workspace { get; set; }

        [Required]
        public int ResourceID { get; set; }

        [ForeignKey("ResourceID")]
        public Resource Resource { get; set; }

        [Required]
        [StringLength(50)]
        public string EmployeeCode { get; set; }

        [Required]
        public int WorkspaceRoleID { get; set; }

        [ForeignKey("WorkspaceRoleID")]
        public WorkspaceRole WorkspaceRole { get; set; }

        public decimal BaseSalaryMonth { get; set; } = 0;
        public decimal OTRatePerHour { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Active"; // 'Active', 'Deactivated', 'Pending_Invite'

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
