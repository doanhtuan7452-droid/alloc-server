using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("RolePermissions")]
    public class RolePermission
    {
        public int WorkspaceRoleID { get; set; }

        [StringLength(50)]
        public string PermissionID { get; set; } = string.Empty;

        [ForeignKey("WorkspaceRoleID")]
        public WorkspaceRole? WorkspaceRole { get; set; }

        [ForeignKey("PermissionID")]
        public WorkspacePermission? WorkspacePermission { get; set; }
    }
}
